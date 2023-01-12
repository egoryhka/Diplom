using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;

namespace Diplom.DataModule
{
    public static class DataManager
    {
        public static Data CurrentData { get; set; }

        public static byte[] Colors = new byte[0];
        public static byte[] MaskedColors = new byte[0];
        
        public static Mask StrainMask = new Mask();
        
        public static Mask GrainMask = new Mask();
        public static Grain[] RawGrains = new Grain[0];
        
        public static Euler[] RawEulers = new Euler[0];
        public static Euler[] BufferEulers = new Euler[0];
        
        public static int[] RawPhaseIndexes = new int[0];
        // ----------------------------------------------

        public static Grain GetGrainByCoords(Vector2Int coords) => RawGrains.FirstOrDefault(x =>
           x.Points.Contains(new Vector2(coords.x, coords.y)) || x.Edges.Contains(new Vector2(coords.x, coords.y)));

        public static string ProjectName
        {
            get
            {
                string name = "Проект: ";
                string projectFileName = Path.GetFileName(currentPathToFile);
                name += projectFileName.Remove(projectFileName.Length - 4, 4);
                return name;
            }
        }

        public static bool HasCurrentPathToFile => !string.IsNullOrEmpty(currentPathToFile);

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
            try
            {
                using (JsonTextReader reader = new JsonTextReader(new StreamReader(pathToFile)))
                {

                    CurrentData = serializer.Deserialize<Data>(reader);
                    currentPathToFile = pathToFile;
                }
            }
            catch
            {
                throw new JsonLoadException();
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

                        CurrentData.Settings.NmPpx = float.Parse(rows[1].ItemArray[2].ToString());

                        CurrentData.Settings.Phases.Clear();
                        foreach (int phaseIndex in CurrentData.Points.Select(x => x.Phase).Distinct().OrderBy(x => x))
                        {
                            CurrentData.Settings.Phases.Add(new Phase() { Index = phaseIndex, Name = "phaseName", Color = System.Windows.Media.Color.FromArgb(255, 255, 255, 255) });
                        }

                        CurrentData.Initialize();
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
    public class JsonLoadException : Exception { }
}
