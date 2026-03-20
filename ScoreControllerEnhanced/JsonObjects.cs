using System;
using System.Collections.Generic;
using System.Linq;

namespace ScoreControllerEnhanced
{
    public class JsonRoot
    {
        public List<JsonCollection> Collections { get; set; }
        public List<JsonProfile> Profiles { get; set; }
        public static string[] GetCollectionTitles(JsonRoot root)
        {
            return root?.Collections?
                .Select(x => x.Title)
                .ToArray()
                ?? Array.Empty<string>();
        }

        public static string[] GetTrackTitles(JsonRoot root, int collectionIndex)
        {
            if (root?.Collections == null)
                return Array.Empty<string>();

            if (collectionIndex < 0 || collectionIndex >= root.Collections.Count)
                return Array.Empty<string>();

            return root.Collections[collectionIndex].Tracks?
                .Select(x => x.Title)
                .ToArray()
                ?? Array.Empty<string>();
        }

        public static string[] GetProfileNames(JsonRoot root, int collectionIndex, int trackIndex)
        {
            if (root?.Collections == null)
                return Array.Empty<string>();

            if (collectionIndex < 0 || collectionIndex >= root.Collections.Count)
                return Array.Empty<string>();

            var tracks = root.Collections[collectionIndex].Tracks;
            if (tracks == null || trackIndex < 0 || trackIndex >= tracks.Count)
                return Array.Empty<string>();

            return tracks[trackIndex].ProfileNames?.ToArray() ?? Array.Empty<string>();
        }

        public static JsonProfile FindProfileByName(JsonRoot root, string name)
        {
            if (root?.Profiles == null || string.IsNullOrWhiteSpace(name))
                return null;

            return root.Profiles.Find(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public static string[] GetProfileEventNames(JsonRoot root, int collectionIndex, int trackIndex, int profileIndex)
        {
            var profileNames = GetProfileNames(root, collectionIndex, trackIndex);
            if (profileIndex < 0 || profileIndex >= profileNames.Length)
                return Array.Empty<string>();

            var profile = FindProfileByName(root, profileNames[profileIndex]);
            return profile?.Events?
                .Select(x => x.Name)
                .ToArray()
                ?? Array.Empty<string>();
        }
    }

    public class JsonCollection
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Event { get; set; }
        public List<JsonTrack> Tracks { get; set; }
    }

    public class JsonTrack
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Event { get; set; }

        // ссылки на профили по имени
        public List<string> ProfileNames { get; set; }
    }

    public class JsonProfile
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public List<JsonProfileEvent> Events { get; set; }
    }

    public class JsonProfileEvent
    {
        public string Name { get; set; }   // "Low", "Mid", "High", "Stealth", "PhaseA"
        public string Event { get; set; }  // GTA event string
    }
}