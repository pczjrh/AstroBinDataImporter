using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace AstroBin_CSV_Importer
{
    public partial class Form1 : Form
    {
        private List<FitsRecord> _currentRecords = new();
        private string? _databaseFolder;

        public Form1()
        {
            InitializeComponent();

            // Lock the form's width so the table can't reveal grey space on resize,
            // while still allowing the form (and table) to grow/shrink vertically.
            MinimumSize = new Size(Width, MinimumSize.Height);
            MaximumSize = new Size(Width, int.MaxValue);

            // A database must be created/opened before any FITS/XISF data can be loaded.
            btn_LoadFITS.Enabled = false;
        }

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

                MessageBox.Show(this,
                    $"Database ready at:\n{FitsDatabase.GetDatabasePath(_databaseFolder)}",
                    "Database Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred while creating the database:\n{ex.Message}",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (_databaseFolder is null)
            {
                MessageBox.Show(this, "Create or open a database before loading FITS/XISF files.",
                    "Database Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            dgv_SummaryData.Rows.Clear();
            pb_Progress.Minimum = 0;
            pb_Progress.Maximum = ofd.FileNames.Length;
            pb_Progress.Value = 0;

            var progress = new Progress<int>(value => pb_Progress.Value = value);

            try
            {
                List<FitsRecord> records = await Task.Run(() => ReadImageFiles(ofd.FileNames, progress));
                _currentRecords = records;
                PopulateSummary(records);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred while reading FITS/XISF files:\n{ex.Message}",
                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btn_LoadFITS.Enabled = true;
                pb_Progress.Value = pb_Progress.Minimum;
            }
        }

        private void btn_SaveDatabase_Click(object sender, EventArgs e)
        {
            if (_databaseFolder is null)
            {
                MessageBox.Show(this, "Create or open a database before saving.",
                    "Database Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_currentRecords.Count == 0)
            {
                MessageBox.Show(this, "Load FITS or XISF files before saving to the database.",
                    "Nothing to Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                MessageBox.Show(this,
                    $"Database saved to:\n{FitsDatabase.GetDatabasePath(_databaseFolder)}\n\n" +
                    $"New records added: {inserted}\nTotal records: {merged.Count}",
                    "Database Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred while saving the database:\n{ex.Message}",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void PopulateSummary(List<FitsRecord> records)
        {
            dgv_SummaryData.Rows.Clear();

            var groups = records
                .GroupBy(r => new { r.Filter, r.DateOnly })
                .OrderBy(g => g.Key.DateOnly, StringComparer.Ordinal)
                .ThenBy(g => g.Key.Filter, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                int frameCount = group.Count();
                double totalExposure = group.Sum(r => r.ExposureSeconds);
                double avgCcdTemp = group.Average(r => r.CcdTemp);
                string ccdName = group.Select(r => r.CcdName)
                                      .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "Unknown";

                dgv_SummaryData.Rows.Add(
                    group.Key.Filter,
                    group.Key.DateOnly,
                    frameCount,
                    totalExposure,
                    avgCcdTemp,
                    ccdName);
            }

            if (dgv_SummaryData.Rows.Count == 0)
            {
                dgv_SummaryData.Rows.Add("No valid FITS headers could be read from the selected files.", "", "", "", "", "");
            }

            double totalSeconds = records.Sum(r => r.ExposureSeconds);
            TimeSpan totalIntegrationTime = TimeSpan.FromSeconds(totalSeconds);
            lbl_TotalIntTimeResult.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0:00}:{1:00}:{2:00}",
                (int)totalIntegrationTime.TotalHours,
                totalIntegrationTime.Minutes,
                totalIntegrationTime.Seconds);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void loadDatabaseToolStripMenuItem_Click(object sender, EventArgs e) => btn_CreateDatabase_Click(sender, e);

        private void saveToDatabaseToolStripMenuItem_Click(object sender, EventArgs e) => btn_SaveDatabase_Click(sender, e);

        private void mapFiltersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<FitsRecord> records = _currentRecords.Count > 0
                ? _currentRecords
                : (_databaseFolder is not null ? FitsDatabase.LoadAll(_databaseFolder) : new List<FitsRecord>());

            IEnumerable<string> loadedFilters = records
                .Select(r => r.Filter)
                .Where(f => !string.IsNullOrWhiteSpace(f));

            // Always offer the standard filter set in addition to anything found in loaded data.
            string[] filters = FilterMappingStore.StandardFilters
                .Concat(loadedFilters)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Dictionary<string, int> existingMappings = FilterMappingStore.Load();

            using var form = new FilterMappingForm(filters, existingMappings);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                FilterMappingStore.Save(form.Mappings);

                MessageBox.Show(this, "Filter mappings saved.",
                    "Map Filters", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
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
        /// DATE-OB truncated to just the calendar date portion (yyyy-MM-dd) for grouping purposes.
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

                string filter = ReadKeyword(fptr, "FILTER") ?? "Unknown";
                string dateObs = ReadKeyword(fptr, "DATE-OBS") ?? "Unknown";
                string? exposureRaw = ReadKeyword(fptr, "EXPOSURE");
                string? ccdTempRaw = ReadKeyword(fptr, "CCD-TEMP");
                string? ccdName = ReadKeyword(fptr, "INSTRUME") ?? ReadKeyword(fptr, "INSTRUMENT");

                return new FitsRecord
                {
                    FileName = fileName,
                    Filter = filter,
                    DateObs = dateObs,
                    ExposureSeconds = HeaderValueHelpers.ParseDouble(exposureRaw),
                    CcdTemp = HeaderValueHelpers.ParseDouble(ccdTempRaw),
                    CcdName = ccdName
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
                .GroupBy(r => (r.DateObs, r.Filter, r.ExposureSeconds, r.CcdTemp), HeaderKeyComparer.Instance)
                .Select(g => g.First())
                .ToList();
        }

        /// <summary>
        /// Compares the header-derived dedup key, treating Filter/DateObs case-insensitively
        /// and comparing exposure/temperature with a small tolerance to account for minor
        /// floating-point formatting differences between FITS and XISF representations.
        /// </summary>
        private sealed class HeaderKeyComparer : IEqualityComparer<(string DateObs, string Filter, double ExposureSeconds, double CcdTemp)>
        {
            public static readonly HeaderKeyComparer Instance = new();
            private const double Tolerance = 0.01;

            public bool Equals((string DateObs, string Filter, double ExposureSeconds, double CcdTemp) x,
                                (string DateObs, string Filter, double ExposureSeconds, double CcdTemp) y)
            {
                return string.Equals(x.DateObs, y.DateObs, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Filter, y.Filter, StringComparison.OrdinalIgnoreCase)
                    && Math.Abs(x.ExposureSeconds - y.ExposureSeconds) < Tolerance
                    && Math.Abs(x.CcdTemp - y.CcdTemp) < Tolerance;
            }

            public int GetHashCode((string DateObs, string Filter, double ExposureSeconds, double CcdTemp) obj)
            {
                return HashCode.Combine(
                    obj.DateObs.ToUpperInvariant(),
                    obj.Filter.ToUpperInvariant(),
                    Math.Round(obj.ExposureSeconds, 2),
                    Math.Round(obj.CcdTemp, 2));
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

            string filter = GetKeyword("FILTER") ?? "Unknown";
            string dateObs = GetKeyword("DATE-OBS") ?? "Unknown";
            string? exposureRaw = GetKeyword("EXPOSURE") ?? GetKeyword("EXPTIME");
            string? ccdTempRaw = GetKeyword("CCD-TEMP");
            string? ccdName = GetKeyword("INSTRUME") ?? GetKeyword("INSTRUMENT");

            return new FitsRecord
            {
                FileName = fileName,
                Filter = filter,
                DateObs = dateObs,
                ExposureSeconds = HeaderValueHelpers.ParseDouble(exposureRaw),
                CcdTemp = HeaderValueHelpers.ParseDouble(ccdTempRaw),
                CcdName = ccdName
            };
        }
    }
}
