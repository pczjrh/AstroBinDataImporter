using System.Globalization;

namespace AstroBin_CSV_Importer
{
    /// <summary>
    /// Persists Filter -> numeric code mappings as a simple CSV file ("Name,Code" per line).
    /// The working copy lives in the user's local application data folder so edits survive
    /// between application runs. On first run, it is seeded from the "FilterCodes.txt" file
    /// shipped alongside the application.
    /// </summary>
    internal static class FilterMappingStore
    {
        private const string FileName = "FilterCodes.txt";
        private const string Header = "Name,Code";

        private static readonly string FolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AstroBinDataImporter");

        private static readonly string FilePath = Path.Combine(FolderPath, FileName);

        private static readonly string SeedFilePath = Path.Combine(AppContext.BaseDirectory, FileName);

        /// <summary>
        /// Loads the current Name -> Code mappings, preserving insertion order.
        /// </summary>
        public static List<(string Name, int Code)> Load()
        {
            EnsureSeeded();

            var mappings = new List<(string Name, int Code)>();

            if (!File.Exists(FilePath))
            {
                return mappings;
            }

            foreach (string line in File.ReadAllLines(FilePath))
            {
                if (TryParseLine(line, out string name, out int code))
                {
                    mappings.Add((name, code));
                }
            }

            return mappings;
        }

        /// <summary>
        /// Replaces the stored mappings with the given set, in the given order.
        /// </summary>
        public static void Save(IEnumerable<(string Name, int Code)> mappings)
        {
            Directory.CreateDirectory(FolderPath);

            var lines = new List<string> { Header };
            lines.AddRange(mappings.Select(m => $"{m.Name},{m.Code.ToString(CultureInfo.InvariantCulture)}"));

            File.WriteAllLines(FilePath, lines);
        }

        private static void EnsureSeeded()
        {
            if (File.Exists(FilePath))
            {
                return;
            }

            Directory.CreateDirectory(FolderPath);

            if (File.Exists(SeedFilePath))
            {
                File.Copy(SeedFilePath, FilePath, overwrite: false);
            }
        }

        private static bool TryParseLine(string line, out string name, out int code)
        {
            name = string.Empty;
            code = 0;

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            string trimmed = line.Trim();
            if (trimmed.Equals(Header, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string[] parts = trimmed.Split(',');
            if (parts.Length != 2)
            {
                return false;
            }

            string candidateName = parts[0].Trim();
            if (string.IsNullOrWhiteSpace(candidateName) ||
                !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int candidateCode))
            {
                return false;
            }

            name = candidateName;
            code = candidateCode;
            return true;
        }
    }
}
