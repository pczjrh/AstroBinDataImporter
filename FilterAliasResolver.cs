namespace AstroBin_CSV_Importer
{
    /// <summary>
    /// Normalizes filter name aliases/abbreviations (e.g. "R", "Luminance") to a single
    /// canonical display name (e.g. "Red", "Lum") so that data recorded under different
    /// naming conventions is aggregated together during reporting.
    /// </summary>
    internal static class FilterAliasResolver
    {
        private static readonly Dictionary<string, string> AliasToCanonical = new(StringComparer.OrdinalIgnoreCase)
        {
            ["R"] = "Red",
            ["Red"] = "Red",

            ["G"] = "Green",
            ["Green"] = "Green",

            ["B"] = "Blue",
            ["Blue"] = "Blue",

            ["L"] = "Lum",
            ["Lum"] = "Lum",
            ["Luminance"] = "Lum",
        };

        /// <summary>
        /// Returns the canonical display name for the given filter name, or the original
        /// (trimmed) value if it has no known alias.
        /// </summary>
        public static string ToCanonicalName(string filter)
        {
            string trimmed = filter?.Trim() ?? string.Empty;

            return AliasToCanonical.TryGetValue(trimmed, out string? canonical)
                ? canonical
                : trimmed;
        }
    }
}
