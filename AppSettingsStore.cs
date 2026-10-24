namespace AstroBin_CSV_Importer
{
    /// <summary>
    /// Persists small user-interface preferences (currently just the dark/light theme
    /// choice) as a single-line text file in %AppData%\AstroBinDataImporter\, mirroring
    /// the storage pattern used by <see cref="FilterMappingStore"/>.
    /// </summary>
    internal static class AppSettingsStore
    {
        private static readonly string SettingsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AstroBinDataImporter");

        private static readonly string SettingsFile = Path.Combine(SettingsFolder, "Settings.txt");

        public static AppTheme LoadTheme()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string value = File.ReadAllText(SettingsFile).Trim();
                    if (Enum.TryParse(value, ignoreCase: true, out AppTheme theme))
                    {
                        return theme;
                    }
                }
            }
            catch
            {
                // Fall back to default theme if the settings file can't be read.
            }

            return AppTheme.Dark;
        }

        public static void SaveTheme(AppTheme theme)
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);
                File.WriteAllText(SettingsFile, theme.ToString());
            }
            catch
            {
                // Non-critical: failing to persist the preference shouldn't crash the app.
            }
        }
    }
}
