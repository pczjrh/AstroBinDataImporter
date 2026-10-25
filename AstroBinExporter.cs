using System.Globalization;
using System.Text;

namespace AstroBin_CSV_Importer
{
    /// <summary>
    /// Builds an AstroBin-compatible "Acquisition sessions" CSV export from the currently
    /// loaded <see cref="FitsRecord"/> data, using the Filter -> numeric code mappings
    /// maintained by <see cref="FilterMappingStore"/>.
    /// </summary>
    internal static class AstroBinExporter
    {
        /// <summary>
        /// Columns that are always included, even if blank, since they are required by AstroBin.
        /// </summary>
        private static readonly HashSet<string> RequiredColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "date", "filter", "number", "duration"
        };

        private static readonly string[] ColumnOrder =
        {
            "date", "filter", "number", "duration", "iso", "binning", "gain", "sensorCooling",
            "fNumber", "darks", "flats", "flatDarks", "bias", "bortle", "meanSqm", "meanFwhm", "temperature"
        };

        /// <summary>
        /// Groups the given records into AstroBin acquisition-session rows (one row per
        /// distinct date/filter/duration/settings combination) and renders them as CSV text.
        /// Columns that are blank for every row are omitted from the output entirely.
        /// </summary>
        public static string BuildCsv(IEnumerable<FitsRecord> records)
        {
            List<FitsRecord> allRecords = records.ToList();

            Dictionary<string, int> filterCodes = FilterMappingStore.Load()
                .ToDictionary(m => m.Name, m => m.Code, StringComparer.OrdinalIgnoreCase);

            List<FitsRecord> lightRecords = allRecords.Where(r => r.ImageType == FrameType.Light).ToList();
            List<FitsRecord> darkRecords = allRecords.Where(r => r.ImageType == FrameType.Dark).ToList();
            List<FitsRecord> flatRecords = allRecords.Where(r => r.ImageType == FrameType.Flat).ToList();
            List<FitsRecord> flatDarkRecords = allRecords.Where(r => r.ImageType == FrameType.FlatDark).ToList();
            List<FitsRecord> biasRecords = allRecords.Where(r => r.ImageType == FrameType.Bias).ToList();

            var groups = lightRecords
                .GroupBy(r => new
                {
                    r.DateOnly,
                    Filter = FilterAliasResolver.ToCanonicalName(r.Filter),
                    r.ExposureSeconds,
                    r.Iso,
                    r.Binning,
                    r.Gain,
                    CcdTemp = Math.Round(r.CcdTemp),
                    r.FNumber,
                    r.MeanSqm,
                    r.MeanFwhm,
                    r.AmbientTemp
                })
                .OrderBy(g => g.Key.DateOnly, StringComparer.Ordinal)
                .ThenBy(g => g.Key.Filter, StringComparer.OrdinalIgnoreCase);

            var rows = new List<Dictionary<string, string>>();

            foreach (var group in groups)
            {
                string canonicalFilter = group.Key.Filter;
                int filterCode = filterCodes.TryGetValue(canonicalFilter, out int code) ? code : 0;

                int darks = CountCalibrationFrames(darkRecords, group.Key.DateOnly);
                int flats = CountCalibrationFrames(flatRecords, group.Key.DateOnly, canonicalFilter);
                int flatDarks = CountCalibrationFrames(flatDarkRecords, group.Key.DateOnly);
                int bias = CountCalibrationFrames(biasRecords, group.Key.DateOnly);

                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["date"] = group.Key.DateOnly,
                    ["filter"] = filterCode.ToString(CultureInfo.InvariantCulture),
                    ["number"] = group.Count().ToString(CultureInfo.InvariantCulture),
                    ["duration"] = group.Key.ExposureSeconds.ToString("0.####", CultureInfo.InvariantCulture),
                    ["iso"] = group.Key.Iso?.ToString(CultureInfo.InvariantCulture) ?? "",
                    ["binning"] = group.Key.Binning?.ToString(CultureInfo.InvariantCulture) ?? "",
                    ["gain"] = group.Key.Gain?.ToString("0.##", CultureInfo.InvariantCulture) ?? "",
                    ["sensorCooling"] = group.Key.CcdTemp.ToString("0", CultureInfo.InvariantCulture),
                    ["fNumber"] = group.Key.FNumber?.ToString("0.##", CultureInfo.InvariantCulture) ?? "",
                    ["darks"] = darks > 0 ? darks.ToString(CultureInfo.InvariantCulture) : "",
                    ["flats"] = flats > 0 ? flats.ToString(CultureInfo.InvariantCulture) : "",
                    ["flatDarks"] = flatDarks > 0 ? flatDarks.ToString(CultureInfo.InvariantCulture) : "",
                    ["bias"] = bias > 0 ? bias.ToString(CultureInfo.InvariantCulture) : "",
                    ["bortle"] = "",
                    ["meanSqm"] = group.Key.MeanSqm?.ToString("0.##", CultureInfo.InvariantCulture) ?? "",
                    ["meanFwhm"] = group.Key.MeanFwhm?.ToString("0.##", CultureInfo.InvariantCulture) ?? "",
                    ["temperature"] = group.Key.AmbientTemp?.ToString("0.##", CultureInfo.InvariantCulture) ?? ""
                };

                rows.Add(row);
            }

            // Omit any column that is blank across every row (except the always-required ones).
            string[] columnsToInclude = ColumnOrder
                .Where(column => RequiredColumns.Contains(column) || rows.Any(r => !string.IsNullOrEmpty(r[column])))
                .ToArray();

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(',', columnsToInclude));

            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(',', columnsToInclude.Select(c => EscapeCsvField(row[c]))));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Counts calibration frames (dark/flat/flat-dark/bias) matching the given date and,
        /// optionally, filter (flats are filter-specific; darks/bias/flat-darks are not).
        /// </summary>
        private static int CountCalibrationFrames(List<FitsRecord> calibrationRecords, string dateOnly, string? canonicalFilter = null)
        {
            IEnumerable<FitsRecord> matches = calibrationRecords.Where(r => r.DateOnly == dateOnly);

            if (canonicalFilter is not null)
            {
                matches = matches.Where(r => string.Equals(
                    FilterAliasResolver.ToCanonicalName(r.Filter), canonicalFilter, StringComparison.OrdinalIgnoreCase));
            }

            return matches.Count();
        }

        private static string EscapeCsvField(string field)
        {
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }
    }
}
