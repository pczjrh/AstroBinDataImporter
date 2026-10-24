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

            ["Ha"] = "Ha",
            ["H-Alpha"] = "Ha",
            ["Halpha"] = "Ha",
            ["Hα"] = "Ha",

            ["Oiii"] = "Oiii",
            ["O-III"] = "Oiii",
            ["O3"] = "Oiii",

            ["Sii"] = "Sii",
            ["S-II"] = "Sii",
            ["S2"] = "Sii",
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

        private static readonly Dictionary<string, Color> CanonicalToColor = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Red"] = Color.FromArgb(220, 30, 30),
            ["Green"] = Color.FromArgb(30, 180, 60),
            ["Blue"] = Color.FromArgb(40, 90, 220),
            ["Lum"] = Color.FromArgb(225, 225, 225),
            ["RGB"] = Color.White,

            // Narrowband palettes: related to but visually distinct from their broadband
            // counterparts (Ha = a different shade of red, Oiii = a different shade of blue,
            // Sii = a dark red).
            ["Ha"] = Color.FromArgb(255, 90, 50),
            ["Oiii"] = Color.FromArgb(0, 180, 220),
            ["Sii"] = Color.FromArgb(120, 20, 20),
        };

        private static readonly Color[] FallbackPalette =
        {
            Color.FromArgb(240, 140, 40),
            Color.FromArgb(160, 90, 200),
            Color.FromArgb(210, 190, 60),
            Color.FromArgb(120, 120, 120),
        };

        /// <summary>
        /// Returns the display color associated with the given canonical filter name
        /// (as returned by <see cref="ToCanonicalName"/>). Known broadband/narrowband
        /// filters get fixed, recognizable colors; unknown filters fall back to a small
        /// rotating palette keyed by name so they stay stable across refreshes.
        /// </summary>
        public static Color ToColor(string canonicalName)
        {
            if (CanonicalToColor.TryGetValue(canonicalName, out Color color))
            {
                return color;
            }

            int index = Math.Abs(canonicalName.GetHashCode()) % FallbackPalette.Length;
            return FallbackPalette[index];
        }
    }
}
