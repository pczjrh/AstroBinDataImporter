using System.Text.Json;

namespace AstroBin_CSV_Importer
{
    /// <summary>
    /// Persists Filter -> numeric code mappings to a JSON file in the user's local
    /// application data folder, so mappings survive between application runs
    /// regardless of which project database is currently open.
    /// </summary>
    internal static class FilterMappingStore
    {
        private static readonly string FolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AstroBinDataImporter");

        private static readonly string FilePath = Path.Combine(FolderPath, "FilterMappings.json");

        /// <summary>
        /// The standard filter names that should always be presented for mapping,
        /// even if they haven't appeared in any loaded FITS/XISF files yet.
        /// </summary>
        public static readonly string[] StandardFilters =
        {
            "Luminance",
            "Red",
            "Green",
            "Blue",
            "Ha",
            "Oiii",
            "Sii"
        };

        public static Dictionary<string, int> Load()
        {
            var mappings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(FilePath))
            {
                try
                {
                    string json = File.ReadAllText(FilePath);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                    if (loaded is not null)
                    {
                        foreach (KeyValuePair<string, int> kvp in loaded)
                        {
                            mappings[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load filter mappings: {ex.Message}");
                }
            }

            return mappings;
        }

        public static void Save(IReadOnlyDictionary<string, int> mappings)
        {
            Directory.CreateDirectory(FolderPath);

            string json = JsonSerializer.Serialize(mappings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}
