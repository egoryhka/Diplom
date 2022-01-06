using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }

        public static void Save(string pathToFile)
        {
            if (CurrentData == null) CurrentData = new Data();

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

    }
}
