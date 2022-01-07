using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;

namespace Diplom.DataModule
{
    public static class DataManager
    {
        public static Data CurrentData;

        private static JsonSerializer serializer;

        private static string currentPathToFile;

        static DataManager()
        {
            serializer = JsonSerializer.Create();
            currentPathToFile = "";
            if (CurrentData == null) CurrentData = new Data();
        }

        public static void Save(string pathToFile)
        {
            using (StreamWriter writer = new StreamWriter(pathToFile))
            {
                serializer.Serialize(writer, CurrentData);
                currentPathToFile = pathToFile;
            }
        }

        public static void SaveCurrent()
        {
            if (string.IsNullOrEmpty(currentPathToFile)) throw new FileNotFoundException();

            using (StreamWriter writer = new StreamWriter(currentPathToFile))
            {
                serializer.Serialize(writer, CurrentData);
            }
        }

        public static void Load(string pathToFile)
        {
            using (JsonTextReader reader = new JsonTextReader(new StreamReader(pathToFile)))
            {
                CurrentData = serializer.Deserialize<Data>(reader);
                currentPathToFile = pathToFile;
            }
        }

        public static void LoadEbsdFromExcel(string pathToFile)
        {
            string excelConnectString =
                $@"Provider=Microsoft.ACE.OLEDB.12.0; Data Source={pathToFile};Extended Properties=""Excel 8.0;HDR=YES;""";

            OleDbConnection connection = new OleDbConnection(excelConnectString);
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            DataSet dataSet = new DataSet();

            try
            {
                connection.Open();

                System.Data.DataTable Sheets = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                string sheetName = Sheets.Rows[0][2].ToString().Replace("'", "");
                OleDbCommand command = new OleDbCommand($"Select * From [{sheetName}]", connection);

                adapter.SelectCommand = command;
                adapter.Fill(dataSet);

                if (!ValidateExcel(dataSet.Tables[0].Columns)) throw new ExcelNotValidException();
                else
                {
                    try
                    {
                        CurrentData.Points = new EBSD[dataSet.Tables[0].Rows.Count];
                        var rows = dataSet.Tables[0].AsEnumerable().ToArray();
                        for (int i = 0; i < rows.Length; i++)
                        {
                            var rowData = rows[i].ItemArray;
                            EBSD point = new EBSD();
                            point.Index = Convert.ToInt32(rowData[0]);
                            point.Phase = Convert.ToInt32(rowData[1]);
                            point.Pos.x = float.Parse(rowData[2].ToString());
                            point.Pos.y = float.Parse(rowData[3].ToString());
                            point.Euler.x = float.Parse(rowData[4].ToString());
                            point.Euler.y = float.Parse(rowData[5].ToString());
                            point.Euler.z = float.Parse(rowData[6].ToString());
                            point.MAD = float.Parse(rowData[7].ToString());
                            point.AFI = Convert.ToInt32(rowData[8]);
                            point.BC = Convert.ToInt32(rowData[9]);
                            point.BS = Convert.ToInt32(rowData[10]);
                            point.Status = Convert.ToInt32(rowData[11]);

                            CurrentData.Points[i] = point;
                        }

                        foreach (int phase in CurrentData.Points.Select(x => x.Phase).Distinct().OrderBy(x => x))
                        {
                            CurrentData.Settings.Phases.Add(phase, "phaseName");
                        }
                    }
                    catch { throw new ReadExcelException(); }
                }
            }
            finally
            {
                connection.Close();
                adapter.Dispose();
            }

        }

        private static bool ValidateExcel(DataColumnCollection dataColumns)
        {
            string[] validColumns = new string[] { "Index", "Phase", "Xpos", "Ypos",
                                                   "Euler1(°)", "Euler2(°)", "Euler3(°)",
                                                   "MAD(°)", "AFI", "BC", "BS", "Status", };
            bool valid = true;

            foreach (var col in validColumns)
            {
                if (!dataColumns.Contains(col))
                {
                    valid = false;
                    break;
                }
            }

            return valid;
        }

    }

    public class ExcelNotValidException : Exception { }
    public class ReadExcelException : Exception { }

}
