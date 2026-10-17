using System.Globalization;
using Microsoft.Data.Sqlite;

namespace AstroBin_CSV_Importer
{
    /// <summary>
    /// Persists extracted FITS/XISF header records into a SQLite database, allowing a
    /// project folder to be reloaded later and acting as the source of truth for
    /// deduplication across multiple load sessions.
    /// </summary>
    internal static class FitsDatabase
    {
        private const string FileName = "AstroBinData.sqlite";

        public static string GetDatabasePath(string folder) => Path.Combine(folder, FileName);

        /// <summary>
        /// Ensures the database and schema exist at the given folder.
        /// </summary>
        public static void Initialize(string folder)
        {
            using SqliteConnection connection = OpenConnection(folder);

            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS FitsRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FileName TEXT NOT NULL,
                    Filter TEXT NOT NULL,
                    DateObs TEXT NOT NULL,
                    ExposureSeconds REAL NOT NULL,
                    CcdTemp REAL NOT NULL,
                    CcdName TEXT,
                    ImageType TEXT,
                    Iso INTEGER,
                    Binning INTEGER,
                    Gain REAL,
                    FNumber REAL,
                    AmbientTemp REAL,
                    MeanFwhm REAL,
                    MeanSqm REAL,
                    UNIQUE (DateObs, Filter, ExposureSeconds, CcdTemp)
                );
                """;
            command.ExecuteNonQuery();

            MigrateSchema(connection);
        }

        /// <summary>
        /// Adds any columns introduced after the table was first created, for databases that
        /// already existed before these columns were added. SQLite doesn't support
        /// "ADD COLUMN IF NOT EXISTS", so existing columns are checked via PRAGMA table_info.
        /// </summary>
        private static void MigrateSchema(SqliteConnection connection)
        {
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var pragmaCommand = connection.CreateCommand())
            {
                pragmaCommand.CommandText = "PRAGMA table_info(FitsRecords);";
                using SqliteDataReader reader = pragmaCommand.ExecuteReader();
                while (reader.Read())
                {
                    existingColumns.Add(reader.GetString(1));
                }
            }

            (string Column, string Definition)[] newColumns =
            {
                ("ImageType", "TEXT"),
                ("Iso", "INTEGER"),
                ("Binning", "INTEGER"),
                ("Gain", "REAL"),
                ("FNumber", "REAL"),
                ("AmbientTemp", "REAL"),
                ("MeanFwhm", "REAL"),
                ("MeanSqm", "REAL")
            };

            foreach ((string column, string definition) in newColumns)
            {
                if (existingColumns.Contains(column))
                {
                    continue;
                }

                using var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = $"ALTER TABLE FitsRecords ADD COLUMN {column} {definition};";
                alterCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Loads all records currently stored in the database.
        /// </summary>
        public static List<FitsRecord> LoadAll(string folder)
        {
            Initialize(folder);

            using SqliteConnection connection = OpenConnection(folder);
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT FileName, Filter, DateObs, ExposureSeconds, CcdTemp, CcdName, ImageType,
                       Iso, Binning, Gain, FNumber, AmbientTemp, MeanFwhm, MeanSqm
                FROM FitsRecords;
                """;

            var records = new List<FitsRecord>();
            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                records.Add(new FitsRecord
                {
                    FileName = reader.GetString(0),
                    Filter = reader.GetString(1),
                    DateObs = reader.GetString(2),
                    ExposureSeconds = reader.GetDouble(3),
                    CcdTemp = reader.GetDouble(4),
                    CcdName = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ImageType = reader.IsDBNull(6)
                        ? FrameType.Light
                        : Enum.TryParse(reader.GetString(6), out FrameType frameType) ? frameType : FrameType.Light,
                    Iso = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    Binning = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    Gain = reader.IsDBNull(9) ? null : reader.GetDouble(9),
                    FNumber = reader.IsDBNull(10) ? null : reader.GetDouble(10),
                    AmbientTemp = reader.IsDBNull(11) ? null : reader.GetDouble(11),
                    MeanFwhm = reader.IsDBNull(12) ? null : reader.GetDouble(12),
                    MeanSqm = reader.IsDBNull(13) ? null : reader.GetDouble(13)
                });
            }

            return records;
        }

        /// <summary>
        /// Deletes all records from the database, emptying it while keeping the file/schema intact.
        /// </summary>
        public static void ClearAll(string folder)
        {
            Initialize(folder);

            using SqliteConnection connection = OpenConnection(folder);
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM FitsRecords;";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Inserts any records not already present (matched on the same header-based key used
        /// for in-memory deduplication: DateObs, Filter, ExposureSeconds, CcdTemp). Existing
        /// matching rows are left untouched, so re-saving previously imported files is a no-op.
        /// </summary>
        /// <returns>The number of new rows actually inserted.</returns>
        public static int SaveRecords(string folder, IEnumerable<FitsRecord> records)
        {
            Initialize(folder);

            using SqliteConnection connection = OpenConnection(folder);
            using SqliteTransaction transaction = connection.BeginTransaction();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT OR IGNORE INTO FitsRecords
                    (FileName, Filter, DateObs, ExposureSeconds, CcdTemp, CcdName, ImageType,
                     Iso, Binning, Gain, FNumber, AmbientTemp, MeanFwhm, MeanSqm)
                VALUES
                    ($fileName, $filter, $dateObs, $exposureSeconds, $ccdTemp, $ccdName, $imageType,
                     $iso, $binning, $gain, $fNumber, $ambientTemp, $meanFwhm, $meanSqm);
                """;

            var fileNameParam = command.Parameters.Add("$fileName", SqliteType.Text);
            var filterParam = command.Parameters.Add("$filter", SqliteType.Text);
            var dateObsParam = command.Parameters.Add("$dateObs", SqliteType.Text);
            var exposureParam = command.Parameters.Add("$exposureSeconds", SqliteType.Real);
            var ccdTempParam = command.Parameters.Add("$ccdTemp", SqliteType.Real);
            var ccdNameParam = command.Parameters.Add("$ccdName", SqliteType.Text);
            var imageTypeParam = command.Parameters.Add("$imageType", SqliteType.Text);
            var isoParam = command.Parameters.Add("$iso", SqliteType.Integer);
            var binningParam = command.Parameters.Add("$binning", SqliteType.Integer);
            var gainParam = command.Parameters.Add("$gain", SqliteType.Real);
            var fNumberParam = command.Parameters.Add("$fNumber", SqliteType.Real);
            var ambientTempParam = command.Parameters.Add("$ambientTemp", SqliteType.Real);
            var meanFwhmParam = command.Parameters.Add("$meanFwhm", SqliteType.Real);
            var meanSqmParam = command.Parameters.Add("$meanSqm", SqliteType.Real);

            int inserted = 0;
            foreach (FitsRecord record in records)
            {
                fileNameParam.Value = record.FileName;
                filterParam.Value = record.Filter;
                dateObsParam.Value = record.DateObs;
                exposureParam.Value = record.ExposureSeconds;
                ccdTempParam.Value = record.CcdTemp;
                ccdNameParam.Value = (object?)record.CcdName ?? DBNull.Value;
                imageTypeParam.Value = record.ImageType.ToString();
                isoParam.Value = (object?)record.Iso ?? DBNull.Value;
                binningParam.Value = (object?)record.Binning ?? DBNull.Value;
                gainParam.Value = (object?)record.Gain ?? DBNull.Value;
                fNumberParam.Value = (object?)record.FNumber ?? DBNull.Value;
                ambientTempParam.Value = (object?)record.AmbientTemp ?? DBNull.Value;
                meanFwhmParam.Value = (object?)record.MeanFwhm ?? DBNull.Value;
                meanSqmParam.Value = (object?)record.MeanSqm ?? DBNull.Value;

                inserted += command.ExecuteNonQuery();
            }

            transaction.Commit();
            return inserted;
        }

        private static SqliteConnection OpenConnection(string folder)
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = GetDatabasePath(folder),
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            var connection = new SqliteConnection(builder.ConnectionString);
            connection.Open();
            return connection;
        }
    }
}
