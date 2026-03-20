using System;
using System.IO;
using Newtonsoft.Json;

namespace ScoreControllerEnhanced
{
    public static class JsonLoader
    {
        public static JsonRoot Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is null or empty.", nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException("JSON file not found.", path);

            string json = File.ReadAllText(path);

            var root = JsonConvert.DeserializeObject<JsonRoot>(json);

            if (root == null)
                throw new Exception("Deserialized JsonRoot is null.");

            return root;
        }
    }
}