using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace AstroBin_CSV_Importer
{
    public partial class Form1 : Form
    {
        // Records currently shown in the grid/chart and used for export (may include unsaved data).
        private List<FitsRecord> _currentRecords = new();

        // Project folder holding AstroBinData.sqlite; null until a database is created/opened.
        private string? _databaseFolder;

        // Designed client/grid heights, used to work out how tall the form may grow.
        private int _baseClientHeight;
        private int _baseGridHeight;

        public Form1()
        {
            InitializeComponent();

            // NotifyIcon requires an icon to be set before it can display balloon tip
            // notifications; the app doesn't ship a custom icon, so use the system default.
            notifyIcon1.Icon = SystemIcons.Application;

            // Lock the form's width so the table can't reveal grey space on resize,
            // while still allowing the form (and table) to grow/shrink vertically.
            MinimumSize = new Size(Width, MinimumSize.Height);
            MaximumSize = new Size(Width, int.MaxValue);
            _baseClientHeight = ClientSize.Height;
            _baseGridHeight = dgv_SummaryData.Height;

            // A database must be created/opened before any FITS/XISF data can be loaded.
            btn_LoadFITS.Enabled = false;

            AppTheme theme = AppSettingsStore.LoadTheme();
            darkModeToolStripMenuItem.Checked = theme == AppTheme.Dark;
            ApplyTheme(theme);

            // Start with the height capped to the (empty) table; it grows as data is loaded.
            LimitHeightToContent(0);
        }

        /// <summary>
        /// Caps the form's height so it can't grow past the bottom of the summary table's
        /// last row (never below the designed height), and shrinks it if it is already taller.
        /// </summary>
        private void LimitHeightToContent(int rowCount)
        {
            int gridNeeded = dgv_SummaryData.HeaderRowHeight + (rowCount * dgv_SummaryData.RowHeight) + 4;
            int chrome = _baseClientHeight - _baseGridHeight;
            int maxClientHeight = Math.Max(_baseClientHeight, gridNeeded + chrome);
            int frameHeight = Height - ClientSize.Height;

            // Setting MaximumSize also clamps the current size if the form is already taller.
            MaximumSize = new Size(Width, maxClientHeight + frameHeight);
        }

        /// <summary>Applies the theme to the whole form (Syncfusion skin + standard controls).</summary>
        private void ApplyTheme(AppTheme theme)
        {
            ThemeManager.Apply(this, theme);
        }

        private void darkModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AppTheme theme = darkModeToolStripMenuItem.Checked ? AppTheme.Dark : AppTheme.Light;
            ApplyTheme(theme);
            AppSettingsStore.SaveTheme(theme);
        }

        /// <summary>Shows a tray balloon; the app's standard non-blocking way to report status.</summary>
        private void ShowNotification(string title, string message)
        {
            notifyIcon1.BalloonTipTitle = title;
            notifyIcon1.BalloonTipText = message;
            notifyIcon1.ShowBalloonTip(3000);
        }

        /// <summary>Creates or opens a project database in a chosen folder and loads any stored records.</summary>
        private void btn_CreateDatabase_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Select (or create) a folder for the project database",
                UseDescriptionForTitle = true
            };

            if (fbd.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                return;
            }

            try
            {
                // Creates the SQLite file and schema immediately, before any data is loaded.
                FitsDatabase.Initialize(fbd.SelectedPath);
                _databaseFolder = fbd.SelectedPath;

                // Reload any records already stored in the database so a re-opened project
                // folder shows its existing data right away.
                _currentRecords = FitsDatabase.LoadAll(_databaseFolder);
                PopulateSummary(_currentRecords);

                btn_LoadFITS.Enabled = true;

                ShowNotification("Database Ready", $"Database ready at:\n{FitsDatabase.GetDatabasePath(_databaseFolder)}");
            }
            catch (Exception ex)
            {
                ShowNotification("Database Error", $"An error occurred while creating the database:\n{ex.Message}");
            }
        }

        /// <summary>Reads headers from the chosen FITS/XISF files off the UI thread and merges them with stored data.</summary>
        private async void btn_LoadFITS_Click(object sender, EventArgs e)
        {
            if (_databaseFolder is null)
            {
                ShowNotification("Database Required", "Create or open a database before loading FITS/XISF files.");
                return;
            }

            using var ofd = new OpenFileDialog
            {
                Title = "Select FITS or XISF files",
                Filter = "FITS/XISF files (*.fits;*.fit;*.fts;*.xisf)|*.fits;*.fit;*.fts;*.xisf|" +
                         "FITS files (*.fits;*.fit;*.fts)|*.fits;*.fit;*.fts|" +
                         "XISF files (*.xisf)|*.xisf|" +
                         "All files (*.*)|*.*",
                Multiselect = true
            };

            if (ofd.ShowDialog(this) != DialogResult.OK || ofd.FileNames.Length == 0)
            {
                return;
            }

            btn_LoadFITS.Enabled = false;
            dgv_SummaryData.DataSource = new List<SummaryRow>();
            pb_Progress.Minimum = 0;
            pb_Progress.Maximum = ofd.FileNames.Length;
            pb_Progress.Value = 0;

            var progress = new Progress<int>(value => pb_Progress.Value = value);

            try
            {
                List<FitsRecord> records = await Task.Run(() => ReadImageFiles(ofd.FileNames, progress));

                // Merge with any records already stored in the database so the table reflects
                // the full picture (previously saved data + newly loaded files) immediately,
                // rather than only showing the newly loaded batch.
                List<FitsRecord> existing = FitsDatabase.LoadAll(_databaseFolder);
                List<FitsRecord> merged = FitsRecordDeduplicator.Deduplicate(existing.Concat(records));

                _currentRecords = merged;
                PopulateSummary(merged);
            }
            catch (Exception ex)
            {
                // The grid was cleared before reading started; restore it from the records we still hold.
                PopulateSummary(_currentRecords);
                ShowNotification("Import Error", $"An error occurred while reading FITS/XISF files:\n{ex.Message}");
            }
            finally
            {
                btn_LoadFITS.Enabled = true;
                pb_Progress.Value = pb_Progress.Minimum;
            }
        }

        /// <summary>Persists the current records to SQLite (duplicates are ignored by the database).</summary>
        private void btn_SaveDatabase_Click(object sender, EventArgs e)
        {
            if (_databaseFolder is null)
            {
                ShowNotification("Database Required", "Create or open a database before saving.");
                return;
            }

            if (_currentRecords.Count == 0)
            {
                ShowNotification("Nothing to Save", "Load FITS or XISF files before saving to the database.");
                return;
            }

            try
            {
                // Merge with any records already stored in the database (if re-opening an
                // existing project folder) so deduplication also accounts for prior saves.
                List<FitsRecord> existing = FitsDatabase.LoadAll(_databaseFolder);
                List<FitsRecord> merged = FitsRecordDeduplicator.Deduplicate(existing.Concat(_currentRecords));

                int inserted = FitsDatabase.SaveRecords(_databaseFolder, merged);

                _currentRecords = merged;
                PopulateSummary(merged);

                ShowNotification("Database Saved",
                    $"Database saved to:\n{FitsDatabase.GetDatabasePath(_databaseFolder)}\n\n" +
                    $"New records added: {inserted}\nTotal records: {merged.Count}");
            }
            catch (Exception ex)
            {
                ShowNotification("Database Error", $"An error occurred while saving the database:\n{ex.Message}");
            }
        }

        private static List<FitsRecord> ReadImageFiles(string[] fileNames, IProgress<int> progress)
        {
            var records = new List<FitsRecord>(fileNames.Length);

            for (int i = 0; i < fileNames.Length; i++)
            {
                try
                {
                    string extension = Path.GetExtension(fileNames[i]);
                    FitsRecord record = extension.Equals(".xisf", StringComparison.OrdinalIgnoreCase)
                        ? XisfReader.ReadHeader(fileNames[i])
                        : FitsReader.ReadHeader(fileNames[i]);

                    records.Add(record);
                }
                catch (Exception ex)
                {
                    // Skip unreadable/corrupt files but keep processing the rest.
                    System.Diagnostics.Debug.WriteLine($"Failed to read '{fileNames[i]}': {ex.Message}");
                }
                finally
                {
                    progress.Report(i + 1);
                }
            }

            // Deduplicate records describing the same exposure. Two passes are used:
            // 1) Match by base file name (e.g. "Light_001.fits" / "Light_001.xisf" pairs).
            // 2) Match by header content (DATE-OBS, FILTER, EXPOSURE, CCD-TEMP), which catches
            //    FITS/XISF pairs (or duplicate imports) that don't share a common file name but
            //    describe the same frame, since the header information should be identical.
            return FitsRecordDeduplicator.Deduplicate(records);
        }

        /// <summary>Rebuilds the summary grid, total integration time and chart from the given records.</summary>
        private void PopulateSummary(List<FitsRecord> records)
        {
            // Only light frames represent actual imaging data for the session summary;
            // calibration frames (dark/bias/flat/flat-dark) are tracked separately and
            // rolled into the AstroBin export's darks/flats/flatDarks/bias counts.
            List<FitsRecord> lightRecords = records.Where(r => r.ImageType == FrameType.Light).ToList();

            var groups = lightRecords
                .GroupBy(r => new { Filter = FilterAliasResolver.ToCanonicalName(r.Filter), r.DateOnly })
                .OrderBy(g => g.Key.DateOnly, StringComparer.Ordinal)
                .ThenBy(g => g.Key.Filter, StringComparer.OrdinalIgnoreCase);

            var rows = new List<SummaryRow>();
            foreach (var group in groups)
            {
                int frameCount = group.Count();
                double totalExposure = group.Sum(r => r.ExposureSeconds);
                double avgCcdTemp = group.Average(r => r.CcdTemp);
                string ccdName = group.Select(r => r.CcdName)
                                      .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "Unknown";

                rows.Add(new SummaryRow(
                    group.Key.Filter,
                    group.Key.DateOnly,
                    frameCount,
                    totalExposure,
                    avgCcdTemp,
                    ccdName));
            }

            if (rows.Count == 0)
            {
                rows.Add(new SummaryRow("No light frames to display.", "", null, null, null, ""));
            }

            dgv_SummaryData.DataSource = rows;
            LimitHeightToContent(rows.Count);

            double totalSeconds = lightRecords.Sum(r => r.ExposureSeconds);
            TimeSpan totalIntegrationTime = TimeSpan.FromSeconds(totalSeconds);
            lbl_TotalIntTimeResult.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0:00}:{1:00}:{2:00}",
                (int)totalIntegrationTime.TotalHours,
                totalIntegrationTime.Minutes,
                totalIntegrationTime.Seconds);

            UpdateOverviewChart(lightRecords);
        }

        /// <summary>Feeds the pie chart one slice per canonical filter, largest first.</summary>
        private void UpdateOverviewChart(List<FitsRecord> lightRecords)
        {
            var filterCounts = lightRecords
                .GroupBy(r => FilterAliasResolver.ToCanonicalName(r.Filter))
                .OrderByDescending(g => g.Count())
                .ToList();

            var slices = new List<PieSlice>();
            for (int i = 0; i < filterCounts.Count; i++)
            {
                var group = filterCounts[i];
                Color color = FilterAliasResolver.ToColor(group.Key);
                slices.Add(new PieSlice(group.Key, group.Count(), color));
            }

            pieChart.SetData(slices);
        }

        /// <summary>Writes the AstroBin CSV next to the database and copies it to the clipboard.</summary>
        private void btn_Export_Click(object sender, EventArgs e)
        {
            if (_databaseFolder is null)
            {
                ShowNotification("Database Required", "Create or open a database before exporting.");
                return;
            }

            if (_currentRecords.Count == 0)
            {
                ShowNotification("Nothing to Export", "Load FITS or XISF files (or a saved database) before exporting.");
                return;
            }

            try
            {
                string csv = AstroBinExporter.BuildCsv(_currentRecords);

                string exportPath = Path.Combine(_databaseFolder, "AstroBinExport.csv");
                File.WriteAllText(exportPath, csv);

                Clipboard.SetText(csv);

                ShowNotification("Export Complete",
                    $"Export saved to:\n{exportPath}\n\nThe data has also been copied to the clipboard.");
            }
            catch (Exception ex)
            {
                ShowNotification("Export Error", $"An error occurred while exporting:\n{ex.Message}");
            }
        }

        private void loadDatabaseToolStripMenuItem_Click(object sender, EventArgs e) => btn_CreateDatabase_Click(sender, e);

        private void saveToDatabaseToolStripMenuItem_Click(object sender, EventArgs e) => btn_SaveDatabase_Click(sender, e);

        private void mapFiltersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<(string Name, int Code)> existingMappings = FilterMappingStore.Load();

            using var form = new FilterMappingForm(existingMappings);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                FilterMappingStore.Save(form.Mappings);

                ShowNotification("Map Filters", "Filter mappings saved.");
            }
        }

        private void clearDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_databaseFolder is null)
            {
                ShowNotification("Database Required", "Create or open a database before clearing it.");
                return;
            }

            DialogResult confirm = MessageBox.Show(this,
                "This will permanently delete all records from the current database. Continue?",
                "Clear Database", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                FitsDatabase.ClearAll(_databaseFolder);

                _currentRecords = new List<FitsRecord>();
                PopulateSummary(_currentRecords);

                ShowNotification("Database Cleared", "All records have been removed from the database.");
            }
            catch (Exception ex)
            {
                ShowNotification("Database Error", $"An error occurred while clearing the database:\n{ex.Message}");
            }
        }
    }

    /// <summary>
    /// Classification of a frame's purpose, derived from the FITS "IMAGETYP" header keyword.
    /// </summary>
    internal enum FrameType
    {
        Light,
        Dark,
        Bias,
        Flat,
        FlatDark,
        Unknown
    }

    /// <summary>
    /// Aggregated data read from a single FITS file's header.
    /// </summary>
    internal sealed class FitsRecord
    {
        public required string FileName { get; init; }
        public required string Filter { get; init; }
        public required string DateObs { get; init; }
        public double ExposureSeconds { get; init; }
        public double CcdTemp { get; init; }
        public string? CcdName { get; init; }

        /// <summary>
        /// Normalized frame type derived from the FITS "IMAGETYP" header keyword
        /// (e.g. "Light Frame", "Dark", "Bias", "Flat"/"Flat Field").
        /// </summary>
        public FrameType ImageType { get; init; } = FrameType.Light;

        /// <summary>
        /// ISO setting on the camera, if present in the header (e.g. "ISO" or "ISOSPEED").
        /// </summary>
        public int? Iso { get; init; }

        /// <summary>
        /// Pixel binning (e.g. "XBINNING"), if present in the header.
        /// </summary>
        public int? Binning { get; init; }

        /// <summary>
        /// Camera gain setting, if present in the header (e.g. "GAIN" or "EGAIN").
        /// </summary>
        public double? Gain { get; init; }

        /// <summary>
        /// Focal ratio (f-number) of the optical system, if present in the header (e.g. "FOCRATIO").
        /// </summary>
        public double? FNumber { get; init; }

        /// <summary>
        /// Ambient (outside air) temperature in Celsius, if present in the header
        /// (e.g. "AMBTEMP" or "OUTTEMP").
        /// </summary>
        public double? AmbientTemp { get; init; }

        /// <summary>
        /// Mean Full Width at Half Maximum (seeing), in arc seconds, if present in the header.
        /// </summary>
        public double? MeanFwhm { get; init; }

        /// <summary>
        /// Mean Sky Quality Meter reading (mag/arcsec^2), if present in the header.
        /// </summary>
        public double? MeanSqm { get; init; }

        /// <summary>
        /// DATE-OBS truncated to just the calendar date portion (yyyy-MM-dd) for grouping purposes.
        /// </summary>
        public string DateOnly => DateObs.Length >= 10 ? DateObs[..10] : DateObs;
    }

    /// <summary>
    /// Thin wrapper around cfitsio.dll (standard CFITSIO API) for reading FITS header keywords.
    /// </summary>
    internal static class FitsReader
    {
        private const int Readonly = 0;
        private const int KeywordLength = 72;
        private const int CommentLength = 73;

        public static FitsRecord ReadHeader(string fileName)
        {
            IntPtr fptr = IntPtr.Zero;
            int status = 0;

            try
            {
                NativeMethods.fits_open_file(out fptr, fileName, Readonly, out status);
                ThrowIfError(status, "opening file");

                string? filterRaw = ReadKeyword(fptr, "FILTER");
                string? bayerPatternRaw = ReadKeyword(fptr, "BAYERPAT") ?? ReadKeyword(fptr, "BAYER_MATRIX");
                string filter = HeaderValueHelpers.ResolveFilter(filterRaw, bayerPatternRaw);
                string dateObs = ReadKeyword(fptr, "DATE-OBS") ?? "Unknown";
                string? exposureRaw = ReadKeyword(fptr, "EXPOSURE") ?? ReadKeyword(fptr, "EXPTIME");
                string? ccdTempRaw = ReadKeyword(fptr, "CCD-TEMP");
                string? ccdName = ReadKeyword(fptr, "INSTRUME") ?? ReadKeyword(fptr, "INSTRUMENT");
                string? imageTypeRaw = ReadKeyword(fptr, "IMAGETYP");
                string? isoRaw = ReadKeyword(fptr, "ISO");
                string? binningRaw = ReadKeyword(fptr, "XBINNING");
                // Note: ISOSPEED holds the camera's gain setting (not ISO) for these cameras.
                string? gainRaw = ReadKeyword(fptr, "GAIN") ?? ReadKeyword(fptr, "EGAIN") ?? ReadKeyword(fptr, "ISOSPEED");
                string? fNumberRaw = ReadKeyword(fptr, "FOCRATIO");
                string? ambientTempRaw = ReadKeyword(fptr, "AMBTEMP") ?? ReadKeyword(fptr, "OUTTEMP");
                string? fwhmRaw = ReadKeyword(fptr, "FWHM");
                string? sqmRaw = ReadKeyword(fptr, "SQM");

                return new FitsRecord
                {
                    FileName = fileName,
                    Filter = filter,
                    DateObs = dateObs,
                    ExposureSeconds = HeaderValueHelpers.ParseDouble(exposureRaw),
                    CcdTemp = HeaderValueHelpers.ParseDouble(ccdTempRaw),
                    CcdName = ccdName,
                    ImageType = HeaderValueHelpers.NormalizeImageType(imageTypeRaw),
                    Iso = HeaderValueHelpers.ParseNullableInt(isoRaw),
                    Binning = HeaderValueHelpers.ParseNullableInt(binningRaw),
                    Gain = HeaderValueHelpers.ParseNullableDouble(gainRaw),
                    FNumber = HeaderValueHelpers.ParseNullableDouble(fNumberRaw),
                    AmbientTemp = HeaderValueHelpers.ParseNullableDouble(ambientTempRaw),
                    MeanFwhm = HeaderValueHelpers.ParseNullableDouble(fwhmRaw),
                    MeanSqm = HeaderValueHelpers.ParseNullableDouble(sqmRaw)
                };
            }
            finally
            {
                if (fptr != IntPtr.Zero)
                {
                    NativeMethods.fits_close_file(fptr, out status);
                }
            }
        }

        private static string? ReadKeyword(IntPtr fptr, string keyword)
        {
            var value = new StringBuilder(KeywordLength);
            var comment = new StringBuilder(CommentLength);

            int status = 0;
            int result = NativeMethods.fits_read_keyword(fptr, keyword, value, comment, out status);

            if (result != 0 || status != 0)
            {
                return null;
            }

            return HeaderValueHelpers.CleanValue(value.ToString());
        }

        private static void ThrowIfError(int status, string context)
        {
            if (status != 0)
            {
                throw new InvalidOperationException($"CFITSIO error {status} while {context}.");
            }
        }

        private static class NativeMethods
        {
            // Note: cfitsio.dll only exports the short (original) CFITSIO symbol names
            // (e.g. "ffopen", "ffclos", "ffgkey"). The "fits_*" names used in fitsio.h
            // are just preprocessor macros and are not present as actual DLL exports,
            // so EntryPoint must be set explicitly to the real exported names.
            [DllImport("cfitsio.dll", EntryPoint = "ffopen", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            public static extern int fits_open_file(out IntPtr fptr, string filename, int mode, out int status);

            [DllImport("cfitsio.dll", EntryPoint = "ffclos", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            public static extern int fits_close_file(IntPtr fptr, out int status);

            [DllImport("cfitsio.dll", EntryPoint = "ffgkey", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            public static extern int fits_read_keyword(
                IntPtr fptr,
                string keyname,
                StringBuilder value,
                StringBuilder comment,
                out int status);
        }
    }

    /// <summary>
    /// Deduplicates <see cref="FitsRecord"/> instances describing the same exposure, whether
    /// they come from the same load session, a previously saved SQLite database, or a mix of
    /// FITS/XISF files for the same frame. Two passes are used:
    /// 1) Match by base file name (e.g. "Light_001.fits" / "Light_001.xisf" pairs).
    /// 2) Match by header content (DATE-OBS, FILTER, EXPOSURE, CCD-TEMP), which catches
    ///    pairs that don't share a common file name but describe the same frame, since the
    ///    header information should be identical.
    /// </summary>
    internal static class FitsRecordDeduplicator
    {
        public static List<FitsRecord> Deduplicate(IEnumerable<FitsRecord> records)
        {
            return records
                .GroupBy(r => Path.GetFileNameWithoutExtension(r.FileName), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .GroupBy(r => (r.DateObs, r.Filter, r.ExposureSeconds, r.CcdTemp, r.ImageType), HeaderKeyComparer.Instance)
                .Select(g => g.First())
                .ToList();
        }

        /// <summary>
        /// Compares the header-derived dedup key, treating Filter/DateObs case-insensitively
        /// and comparing exposure/temperature with a small tolerance to account for minor
        /// floating-point formatting differences between FITS and XISF representations.
        /// </summary>
        private sealed class HeaderKeyComparer : IEqualityComparer<(string DateObs, string Filter, double ExposureSeconds, double CcdTemp, FrameType ImageType)>
        {
            public static readonly HeaderKeyComparer Instance = new();
            private const double Tolerance = 0.01;

            public bool Equals((string DateObs, string Filter, double ExposureSeconds, double CcdTemp, FrameType ImageType) x,
                                (string DateObs, string Filter, double ExposureSeconds, double CcdTemp, FrameType ImageType) y)
            {
                return string.Equals(x.DateObs, y.DateObs, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Filter, y.Filter, StringComparison.OrdinalIgnoreCase)
                    && Math.Abs(x.ExposureSeconds - y.ExposureSeconds) < Tolerance
                    && Math.Abs(x.CcdTemp - y.CcdTemp) < Tolerance
                    && x.ImageType == y.ImageType;
            }

            public int GetHashCode((string DateObs, string Filter, double ExposureSeconds, double CcdTemp, FrameType ImageType) obj)
            {
                return HashCode.Combine(
                    obj.DateObs.ToUpperInvariant(),
                    obj.Filter.ToUpperInvariant(),
                    Math.Round(obj.ExposureSeconds, 2),
                    Math.Round(obj.CcdTemp, 2),
                    obj.ImageType);
            }
        }
    }

    /// <summary>
    /// Shared parsing helpers used by both the FITS and XISF header readers, since both
    /// formats represent header values using the same FITS keyword conventions
    /// (e.g. quoted strings, numeric literals).
    /// </summary>
    internal static class HeaderValueHelpers
    {
        public static string CleanValue(string raw)
        {
            string trimmed = raw.Trim();
            if (trimmed.Length >= 2 && trimmed[0] == '\'' && trimmed[^1] == '\'')
            {
                trimmed = trimmed[1..^1].Trim();
            }
            return trimmed;
        }

        public static double ParseDouble(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return 0;
            }

            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? value
                : 0;
        }

        public static double? ParseNullableDouble(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? value
                : null;
        }

        public static int? ParseNullableInt(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            // Some cameras report binning/ISO as a float-looking string (e.g. "100.0"),
            // so parse as double first and round, rather than failing outright.
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? (int)Math.Round(value)
                : null;
        }

        /// <summary>
        /// Maps common "IMAGETYP" header values to a <see cref="FrameType"/>. Different
        /// capture software uses different conventions (e.g. "Light Frame" vs "Light",
        /// "Flat Field" vs "Flat", "Dark Frame" vs "Dark"), so matching is done by prefix.
        /// </summary>
        public static FrameType NormalizeImageType(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return FrameType.Light;
            }

            string value = raw.Trim();

            if (value.StartsWith("FlatDark", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("Flat Dark", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("Dark Flat", StringComparison.OrdinalIgnoreCase))
            {
                return FrameType.FlatDark;
            }

            if (value.StartsWith("Flat", StringComparison.OrdinalIgnoreCase))
            {
                return FrameType.Flat;
            }

            if (value.StartsWith("Dark", StringComparison.OrdinalIgnoreCase))
            {
                return FrameType.Dark;
            }

            if (value.StartsWith("Bias", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("Offset", StringComparison.OrdinalIgnoreCase))
            {
                return FrameType.Bias;
            }

            if (value.StartsWith("Light", StringComparison.OrdinalIgnoreCase))
            {
                return FrameType.Light;
            }

            return FrameType.Unknown;
        }

        /// <summary>
        /// Resolves the effective filter name for a frame. One-shot-color cameras typically
        /// have no "FILTER" keyword (or report a Bayer color matrix via "BAYERPAT"/"BAYER_MATRIX"
        /// instead of a filter name), in which case the frame is effectively unfiltered/color
        /// and should be reported as "RGB" rather than "Unknown".
        /// </summary>
        public static string ResolveFilter(string? filterRaw, string? bayerPatternRaw)
        {
            if (!string.IsNullOrWhiteSpace(bayerPatternRaw))
            {
                return "RGB";
            }

            return string.IsNullOrWhiteSpace(filterRaw) ? "RGB" : filterRaw.Trim();
        }
    }

    /// <summary>
    /// Reader for PixInsight's XISF (Extensible Image Serialization Format) files.
    /// An XISF file begins with an 8-byte "XISF0100" signature, a 4-byte little-endian
    /// header length, 4 reserved bytes, then a UTF-8 XML header. PixInsight stores
    /// original FITS header keywords as &lt;FITSKeyword name="..." value="..." comment="..."/&gt;
    /// elements, so the same keyword names used for FITS files apply here.
    /// See the PixInsight Class Library (PCL) / XISF specification: https://gitlab.com/pixinsight/PCL/
    /// </summary>
    internal static class XisfReader
    {
        private static readonly byte[] Signature = Encoding.ASCII.GetBytes("XISF0100");

        public static FitsRecord ReadHeader(string fileName)
        {
            using var stream = File.OpenRead(fileName);
            using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

            byte[] signature = reader.ReadBytes(Signature.Length);
            if (!signature.AsSpan().SequenceEqual(Signature))
            {
                throw new InvalidOperationException("Not a valid XISF file (signature mismatch).");
            }

            uint headerLength = reader.ReadUInt32();
            reader.ReadBytes(4); // reserved

            byte[] headerBytes = reader.ReadBytes(checked((int)headerLength));
            string xml = Encoding.UTF8.GetString(headerBytes);

            XDocument doc = XDocument.Parse(xml);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var keywordElements = doc.Descendants(ns + "FITSKeyword").ToList();

            string? GetKeyword(string name) =>
                keywordElements
                    .FirstOrDefault(e => string.Equals((string?)e.Attribute("name"), name, StringComparison.OrdinalIgnoreCase))
                    ?.Attribute("value")?.Value is string raw
                    ? HeaderValueHelpers.CleanValue(raw)
                    : null;

            string? filterRaw = GetKeyword("FILTER");
            string? bayerPatternRaw = GetKeyword("BAYERPAT") ?? GetKeyword("BAYER_MATRIX");
            string filter = HeaderValueHelpers.ResolveFilter(filterRaw, bayerPatternRaw);
            string dateObs = GetKeyword("DATE-OBS") ?? "Unknown";
            string? exposureRaw = GetKeyword("EXPOSURE") ?? GetKeyword("EXPTIME");
            string? ccdTempRaw = GetKeyword("CCD-TEMP");
            string? ccdName = GetKeyword("INSTRUME") ?? GetKeyword("INSTRUMENT");
            string? imageTypeRaw = GetKeyword("IMAGETYP");
            string? isoRaw = GetKeyword("ISO");
            string? binningRaw = GetKeyword("XBINNING");
            // Note: ISOSPEED holds the camera's gain setting (not ISO) for these cameras.
            string? gainRaw = GetKeyword("GAIN") ?? GetKeyword("EGAIN") ?? GetKeyword("ISOSPEED");
            string? fNumberRaw = GetKeyword("FOCRATIO");
            string? ambientTempRaw = GetKeyword("AMBTEMP") ?? GetKeyword("OUTTEMP");
            string? fwhmRaw = GetKeyword("FWHM");
            string? sqmRaw = GetKeyword("SQM");

            return new FitsRecord
            {
                FileName = fileName,
                Filter = filter,
                DateObs = dateObs,
                ExposureSeconds = HeaderValueHelpers.ParseDouble(exposureRaw),
                CcdTemp = HeaderValueHelpers.ParseDouble(ccdTempRaw),
                CcdName = ccdName,
                ImageType = HeaderValueHelpers.NormalizeImageType(imageTypeRaw),
                Iso = HeaderValueHelpers.ParseNullableInt(isoRaw),
                Binning = HeaderValueHelpers.ParseNullableInt(binningRaw),
                Gain = HeaderValueHelpers.ParseNullableDouble(gainRaw),
                FNumber = HeaderValueHelpers.ParseNullableDouble(fNumberRaw),
                AmbientTemp = HeaderValueHelpers.ParseNullableDouble(ambientTempRaw),
                MeanFwhm = HeaderValueHelpers.ParseNullableDouble(fwhmRaw),
                MeanSqm = HeaderValueHelpers.ParseNullableDouble(sqmRaw)
            };
        }
    }
}
