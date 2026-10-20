using System;
using System.Collections.Generic;
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

            var root = JsonConvert.DeserializeObject<JsonRoot>(File.ReadAllText(path));
            if (root == null)
                throw new InvalidDataException("Deserialized JsonRoot is null.");

            root.Collections = root.Collections ?? new List<JsonCollection>();
            root.Profiles = root.Profiles ?? new List<JsonProfile>();

            // Optional modular profiles: scripts/Score Controller Enhanced/profiles/*.json.
            // The original score_controls.json and its legacy profile names still work.
            var basePath = Path.GetDirectoryName(Path.GetFullPath(path));
            var directory = Path.Combine(basePath, "profiles");
            if (!Directory.Exists(directory))
                return root;

            foreach (var file in Directory.GetFiles(directory, "*.json"))
            {
                JsonProfilePackage package;
                try
                {
                    package = JsonConvert.DeserializeObject<JsonProfilePackage>(File.ReadAllText(file));
                }
                catch (Exception exception)
                {
                    throw new InvalidDataException("Invalid REL profile package: " + file, exception);
                }

                if (package == null || package.Track == null || package.Profile == null ||
                    string.IsNullOrWhiteSpace(package.Track.Name) ||
                    string.IsNullOrWhiteSpace(package.Profile.Name))
                    throw new InvalidDataException("Incomplete REL profile package: " + file);

                if (root.Profiles.Exists(x =>
                    string.Equals(x?.Name, package.Profile.Name, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException("Duplicate music profile: " + package.Profile.Name);

                var collectionName = string.IsNullOrWhiteSpace(package.CollectionName)
                    ? "REL Profiles" : package.CollectionName;
                var collectionTitle = string.IsNullOrWhiteSpace(package.CollectionTitle)
                    ? collectionName : package.CollectionTitle;

                var collection = root.Collections.Find(x =>
                    string.Equals(x?.Name, collectionName, StringComparison.OrdinalIgnoreCase));
                if (collection == null)
                {
                    collection = new JsonCollection {
                        Name = collectionName,
                        Title = collectionTitle,
                        Tracks = new List<JsonTrack>()
                    };
                    root.Collections.Add(collection);
                }

                collection.Tracks = collection.Tracks ?? new List<JsonTrack>();
                if (collection.Tracks.Exists(x =>
                    string.Equals(x?.Name, package.Track.Name, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException("Duplicate music track: " + package.Track.Name);

                if (package.Track.ProfileNames == null || package.Track.ProfileNames.Count == 0)
                    package.Track.ProfileNames = new List<string> { package.Profile.Name };
                else if (!package.Track.ProfileNames.Exists(x =>
                    string.Equals(x, package.Profile.Name, StringComparison.OrdinalIgnoreCase)))
                    package.Track.ProfileNames.Add(package.Profile.Name);

                root.Profiles.Add(package.Profile);
                collection.Tracks.Add(package.Track);
            }

            return root;
        }
    }
}
