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
                    UNIQUE (DateObs, Filter, ExposureSeconds, CcdTemp)
                );
                """;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Loads all records currently stored in the database.
        /// </summary>
        public static List<FitsRecord> LoadAll(string folder)
        {
            Initialize(folder);

            using SqliteConnection connection = OpenConnection(folder);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT FileName, Filter, DateObs, ExposureSeconds, CcdTemp, CcdName FROM FitsRecords;";

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
                    CcdName = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }

            return records;
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
                INSERT OR IGNORE INTO FitsRecords (FileName, Filter, DateObs, ExposureSeconds, CcdTemp, CcdName)
                VALUES ($fileName, $filter, $dateObs, $exposureSeconds, $ccdTemp, $ccdName);
                """;

            var fileNameParam = command.Parameters.Add("$fileName", SqliteType.Text);
            var filterParam = command.Parameters.Add("$filter", SqliteType.Text);
            var dateObsParam = command.Parameters.Add("$dateObs", SqliteType.Text);
            var exposureParam = command.Parameters.Add("$exposureSeconds", SqliteType.Real);
            var ccdTempParam = command.Parameters.Add("$ccdTemp", SqliteType.Real);
            var ccdNameParam = command.Parameters.Add("$ccdName", SqliteType.Text);

            int inserted = 0;
            foreach (FitsRecord record in records)
            {
                fileNameParam.Value = record.FileName;
                filterParam.Value = record.Filter;
                dateObsParam.Value = record.DateObs;
                exposureParam.Value = record.ExposureSeconds;
                ccdTempParam.Value = record.CcdTemp;
                ccdNameParam.Value = (object?)record.CcdName ?? DBNull.Value;

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
