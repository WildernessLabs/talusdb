using System.Data.SQLite;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WildernessLabs.TalusDB;

namespace TalusDBBenchmark
{
    public struct BenchmarkTelemetry
    {
        public DateTime Timestamp;
        public double Value;
        public int SensorId;
        public double Latitude;
        public double Longitude;

        public override bool Equals(object obj)
        {
            if (obj is BenchmarkTelemetry other)
            {
                return Timestamp.Ticks == other.Timestamp.Ticks &&
                       Math.Abs(Value - other.Value) < 0.001 &&
                       SensorId == other.SensorId &&
                       Math.Abs(Latitude - other.Latitude) < 0.001 &&
                       Math.Abs(Longitude - other.Longitude) < 0.001;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Timestamp, Value, SensorId, Latitude, Longitude);
        }
    }

    public struct BenchmarkTelemetryWithString
    {
        public DateTime Timestamp;
        public double Value;
        public int SensorId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string SensorName;

        public override bool Equals(object obj)
        {
            if (obj is BenchmarkTelemetryWithString other)
            {
                bool timeEqual = DateTime.Equals(
                    Timestamp.ToUniversalTime(),
                    other.Timestamp.ToUniversalTime());

                return timeEqual &&
                       Math.Abs(Value - other.Value) < 0.001 &&
                       SensorId == other.SensorId &&
                       SensorName?.TrimEnd('\0') == other.SensorName?.TrimEnd('\0');
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Timestamp, Value, SensorId, SensorName);
        }
    }

    internal class Program
    {
        // Constants for benchmarks
        private const int SmallDataset = 1_000;
        private const int MediumDataset = 10_000;
        private const int LargeDataset = 100_000;

        // Maximum rows to keep in tables (simulating resource constraints)
        private const int MaxTableSize = 1_000;

        // SQLite database path
        private static readonly string _sqliteDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "benchmark.db");

        // Output file for results
        private static readonly string _resultsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "benchmark_results.txt");

        private static void Main(string[] args)
        {
            Console.WriteLine("=== TalusDB vs SQLite Benchmark (Resource Constrained) ===");
            Console.WriteLine($"Maximum table size: {MaxTableSize} rows");
            Console.WriteLine("Choose a benchmark to run:");
            Console.WriteLine("1. Insertion Performance (TalusDB only)");
            Console.WriteLine("2. Retrieval Performance (TalusDB only)");
            Console.WriteLine("3. String vs Numeric Performance (TalusDB only)");
            Console.WriteLine("4. TalusDB vs SQLite - Small Dataset");
            Console.WriteLine("5. TalusDB vs SQLite - Medium Dataset");
            Console.WriteLine("6. TalusDB vs SQLite - Large Dataset");
            Console.WriteLine("7. Storage Efficiency Comparison");
            Console.WriteLine("8. Run All Benchmarks");
            Console.WriteLine("9. Test TalusDB Stream Behavior (AlwaysNew vs KeepOpen)");
            Console.WriteLine("0. Exit");

            bool exit = false;
            while (!exit)
            {
                Console.Write("> ");
                var input = Console.ReadLine();

                switch (input)
                {
                    case "0":
                        exit = true;
                        break;
                    case "1":
                        BenchmarkInsertionPerformance();
                        break;
                    case "2":
                        BenchmarkRetrievalPerformance();
                        break;
                    case "3":
                        BenchmarkStringPerformance();
                        break;
                    case "4":
                        ComparisonBenchmark(SmallDataset, "Small");
                        break;
                    case "5":
                        ComparisonBenchmark(MediumDataset, "Medium");
                        break;
                    case "6":
                        ComparisonBenchmark(LargeDataset, "Large");
                        break;
                    case "7":
                        CompareStorageEfficiency();
                        break;
                    case "8":
                        RunAllBenchmarks();
                        break;
                    case "9":
                        TestStreamBehavior();
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }

        private static void RunAllBenchmarks()
        {
            Console.WriteLine("Running all benchmarks. This may take several minutes...");

            // Start fresh with results file
            if (File.Exists(_resultsFilePath))
            {
                File.Delete(_resultsFilePath);
            }

            BenchmarkInsertionPerformance();
            BenchmarkRetrievalPerformance();
            BenchmarkStringPerformance();
            ComparisonBenchmark(SmallDataset, "Small");
            ComparisonBenchmark(MediumDataset, "Medium");

            Console.Write("Run large dataset benchmark? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                ComparisonBenchmark(LargeDataset, "Large");
            }

            CompareStorageEfficiency();

            Console.WriteLine($"All benchmarks complete. Results saved to {_resultsFilePath}");
        }

        private static void TestStreamBehavior()
        {
            LogMessage("=== STREAM BEHAVIOR COMPARISON ===");
            LogMessage($"Dataset size: {MediumDataset} records");
            LogMessage($"Max table size: {MaxTableSize} records");
            LogMessage("--------------------------------------");

            // Test AlwaysNew behavior (default)
            LogMessage("Testing with StreamBehavior.AlwaysNew (default):");
            var alwaysNewTime = TestTalusDBWithStreamBehavior(StreamBehavior.AlwaysNew);

            // Test KeepOpen behavior
            LogMessage("Testing with StreamBehavior.KeepOpen:");
            var keepOpenTime = TestTalusDBWithStreamBehavior(StreamBehavior.KeepOpen);

            // Compare results
            double improvement = (double)alwaysNewTime / keepOpenTime;
            LogMessage($"StreamBehavior.KeepOpen is {improvement:F2}x faster than AlwaysNew");
            LogMessage("--------------------------------------");
        }

        private static long TestTalusDBWithStreamBehavior(StreamBehavior behavior)
        {
            var db = new Database();
            DropAllTables(db);

            // Create table with specified max size
            var t = db.CreateTable<BenchmarkTelemetry>(MaxTableSize);

            // Set stream behavior (requires reflection since it's not public)
            var tableType = t.GetType();
            var streamBehaviorProperty = tableType.GetProperty("StreamBehavior");
            streamBehaviorProperty?.SetValue(t, behavior);

            // Benchmark insertion with cycling
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < MediumDataset; i++)
            {
                var item = new BenchmarkTelemetry
                {
                    Timestamp = DateTime.Now.AddMinutes(-i),
                    Value = i * 1.1,
                    SensorId = i % 100,
                    Latitude = 40.0 + (i % 10) * 0.1,
                    Longitude = -74.0 - (i % 10) * 0.1
                };
                t.Insert(item);

                // Maintain max size by removing oldest when full
                if (t.Count >= MaxTableSize)
                {
                    t.Remove();
                }
            }

            sw.Stop();

            long elapsedMs = sw.ElapsedMilliseconds;
            double rate = MediumDataset * 1000.0 / elapsedMs;

            LogMessage($"  Total insertion time: {elapsedMs} ms");
            LogMessage($"  Insertion rate: {rate:F2} records/second");

            return elapsedMs;
        }

        private static void BenchmarkInsertionPerformance()
        {
            LogMessage("=== INSERTION PERFORMANCE BENCHMARK ===");
            LogMessage("Testing insertion performance with varying dataset sizes");
            LogMessage($"Small: {SmallDataset} records");
            LogMessage($"Medium: {MediumDataset} records");
            LogMessage($"Large: {LargeDataset} records");
            LogMessage($"Max table size: {MaxTableSize} records");
            LogMessage("--------------------------------------");

            // Run small dataset test
            RunInsertionBenchmark(SmallDataset, "Small");

            // Run medium dataset test
            RunInsertionBenchmark(MediumDataset, "Medium");

            // Ask about large dataset
            Console.Write("Run large dataset benchmark? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                RunInsertionBenchmark(LargeDataset, "Large");
            }
        }

        private static void BenchmarkRetrievalPerformance()
        {
            LogMessage("=== RETRIEVAL PERFORMANCE BENCHMARK ===");
            LogMessage("Testing retrieval performance with varying dataset sizes");
            LogMessage($"Small: {SmallDataset} records");
            LogMessage($"Medium: {MediumDataset} records");
            LogMessage($"Large: {LargeDataset} records");
            LogMessage($"Max table size: {MaxTableSize} records");
            LogMessage("--------------------------------------");

            // Run small dataset test
            RunRetrievalBenchmark(SmallDataset, "Small");

            // Run medium dataset test
            RunRetrievalBenchmark(MediumDataset, "Medium");

            // Ask about large dataset
            Console.Write("Run large dataset benchmark? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                RunRetrievalBenchmark(LargeDataset, "Large");
            }
        }

        private static void BenchmarkStringPerformance()
        {
            LogMessage("=== STRING PERFORMANCE BENCHMARK ===");
            LogMessage("Testing performance with string fields vs. numeric-only");
            LogMessage($"Dataset size: {MediumDataset} records");
            LogMessage($"Max table size: {MaxTableSize} records");
            LogMessage("--------------------------------------");

            // Run numeric benchmark
            var numericResults = RunCustomBenchmark<BenchmarkTelemetry>(MediumDataset, "Numeric-only");

            // Run string benchmark
            var stringResults = RunCustomBenchmark<BenchmarkTelemetryWithString>(MediumDataset, "With-strings");

            // Calculate difference
            double insertionSpeedDiff = ((stringResults.InsertionRate / numericResults.InsertionRate) - 1) * 100;
            double retrievalSpeedDiff = ((stringResults.RetrievalRate / numericResults.RetrievalRate) - 1) * 100;
            double sizePerRecordDiff = ((stringResults.BytesPerRecord / numericResults.BytesPerRecord) - 1) * 100;

            LogMessage("======= COMPARISON RESULTS =======");
            LogMessage($"Insertion rate difference: {insertionSpeedDiff:F2}% ({(insertionSpeedDiff > 0 ? "slower" : "faster")} with strings)");
            LogMessage($"Retrieval rate difference: {retrievalSpeedDiff:F2}% ({(retrievalSpeedDiff > 0 ? "slower" : "faster")} with strings)");
            LogMessage($"Size per record difference: {sizePerRecordDiff:F2}% ({(sizePerRecordDiff > 0 ? "larger" : "smaller")} with strings)");
        }

        private static void ComparisonBenchmark(int recordCount, string label)
        {
            LogMessage($"=== {label.ToUpper()} DATASET COMPARISON ({recordCount} RECORDS) ===");
            LogMessage($"Max table size: {MaxTableSize} records");

            // Run TalusDB benchmark
            var talusDbResults = RunTalusDBBenchmark(recordCount);

            // Run SQLite benchmark
            var sqliteResults = RunSQLiteBenchmark(recordCount);

            // Calculate comparisons
            double insertRatio = sqliteResults.InsertMs / (double)talusDbResults.InsertMs;
            double retrieveRatio = sqliteResults.RetrieveMs / (double)talusDbResults.RetrieveMs;
            double sizeRatio = sqliteResults.BytesPerRecord / talusDbResults.BytesPerRecord;

            // Output comparison results
            LogMessage("=== COMPARISON RESULTS ===");

            LogMessage("Insertion Performance:");
            if (insertRatio > 1)
            {
                LogMessage($"  TalusDB is {insertRatio:F2}x faster than SQLite");
            }
            else
            {
                LogMessage($"  SQLite is {1 / insertRatio:F2}x faster than TalusDB");
            }

            LogMessage("Retrieval Performance:");
            if (retrieveRatio > 1)
            {
                LogMessage($"  TalusDB is {retrieveRatio:F2}x faster than SQLite");
            }
            else
            {
                LogMessage($"  SQLite is {1 / retrieveRatio:F2}x faster than TalusDB");
            }

            LogMessage("Storage Efficiency:");
            if (sizeRatio > 1)
            {
                LogMessage($"  TalusDB is {sizeRatio:F2}x more storage efficient than SQLite");
            }
            else
            {
                LogMessage($"  SQLite is {1 / sizeRatio:F2}x more storage efficient than TalusDB");
            }

            LogMessage("--------------------------------------");
        }

        private static void CompareStorageEfficiency()
        {
            LogMessage("=== STORAGE EFFICIENCY COMPARISON ===");
            LogMessage($"Dataset size: {MaxTableSize} records (fixed)");
            LogMessage("--------------------------------------");

            // Setup SQLite
            using (var connection = new SQLiteConnection($"Data Source={_sqliteDbPath};Version=3;"))
            {
                connection.Open();

                // Clear existing data
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM sensors";
                    cmd.ExecuteNonQuery();
                }

                // Insert benchmark data (limited to MaxTableSize)
                using (var transaction = connection.BeginTransaction())
                {
                    for (int i = 0; i < MaxTableSize; i++)
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = "INSERT INTO sensors (timestamp, value, sensor_id, latitude, longitude) VALUES (@timestamp, @value, @sensor_id, @latitude, @longitude)";
                            cmd.Parameters.AddWithValue("@timestamp", DateTime.Now.AddMinutes(-i));
                            cmd.Parameters.AddWithValue("@value", i * 1.1);
                            cmd.Parameters.AddWithValue("@sensor_id", i % 100);
                            cmd.Parameters.AddWithValue("@latitude", 40.0 + (i % 10) * 0.1);
                            cmd.Parameters.AddWithValue("@longitude", -74.0 - (i % 10) * 0.1);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }

            // Measure SQLite file size
            var sqliteFileInfo = new FileInfo(_sqliteDbPath);
            long sqliteSize = sqliteFileInfo.Length;
            double sqliteBytesPerRecord = (double)sqliteSize / MaxTableSize;

            // Measure TalusDB file size
            var db = new Database();
            DropAllTables(db);

            var t = db.CreateTable<BenchmarkTelemetry>(MaxTableSize);

            // Insert test data (exactly MaxTableSize records)
            for (int i = 0; i < MaxTableSize; i++)
            {
                var item = new BenchmarkTelemetry
                {
                    Timestamp = DateTime.Now.AddMinutes(-i),
                    Value = i * 1.1,
                    SensorId = i % 100,
                    Latitude = 40.0 + (i % 10) * 0.1,
                    Longitude = -74.0 - (i % 10) * 0.1
                };
                t.Insert(item);
            }

            string dbFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".talusdb");
            string tablePath = Path.Combine(dbFolder, typeof(BenchmarkTelemetry).Name);
            var talusFileInfo = new FileInfo(tablePath);
            long talusSize = talusFileInfo.Length;
            double talusBytesPerRecord = (double)talusSize / MaxTableSize;

            // Output results
            LogMessage($"SQLite storage:");
            LogMessage($"  Total size: {FormatByteSize(sqliteSize)}");
            LogMessage($"  Per record: {sqliteBytesPerRecord:F2} bytes");

            LogMessage($"TalusDB storage:");
            LogMessage($"  Total size: {FormatByteSize(talusSize)}");
            LogMessage($"  Per record: {talusBytesPerRecord:F2} bytes");

            double ratio = sqliteBytesPerRecord / talusBytesPerRecord;

            if (ratio > 1)
            {
                LogMessage($"TalusDB is {ratio:F2}x more storage efficient than SQLite");
            }
            else
            {
                LogMessage($"SQLite is {1 / ratio:F2}x more storage efficient than TalusDB");
            }
        }

        private static void RunInsertionBenchmark(int recordCount, string label)
        {
            var db = new Database();
            DropAllTables(db);

            // Create table with max size constraint
            var t = db.CreateTable<BenchmarkTelemetry>(MaxTableSize);

            // Generate test data
            var items = GenerateTestData<BenchmarkTelemetry>(recordCount);

            // Benchmark insertion with cycling
            var sw = Stopwatch.StartNew();

            foreach (var item in items)
            {
                t.Insert(item);

                // Remove oldest when at capacity
                if (t.Count >= MaxTableSize)
                {
                    t.Remove();
                }
            }

            sw.Stop();

            long elapsedMs = sw.ElapsedMilliseconds;
            double rate = recordCount * 1000.0 / elapsedMs;

            LogMessage($"{label} dataset ({recordCount} records, max {MaxTableSize} at a time):");
            LogMessage($"  Total insertion time: {elapsedMs} ms");
            LogMessage($"  Insertion rate: {rate:F2} records/second");

            // Get file size to calculate storage efficiency
            string dbFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".talusdb");
            string tablePath = Path.Combine(dbFolder, typeof(BenchmarkTelemetry).Name);
            long fileSize = new FileInfo(tablePath).Length;
            double bytesPerRecord = (double)fileSize / Math.Min(recordCount, MaxTableSize);

            LogMessage($"  File size: {FormatByteSize(fileSize)}");
            LogMessage($"  Storage per record: {bytesPerRecord:F2} bytes");
            LogMessage("--------------------------------------");
        }

        private static void RunRetrievalBenchmark(int recordCount, string label)
        {
            var db = new Database();
            DropAllTables(db);

            // Create table with max size constraint
            var t = db.CreateTable<BenchmarkTelemetry>(MaxTableSize);

            // Insert test data (only up to max size)
            var actualRecordCount = Math.Min(recordCount, MaxTableSize);
            var items = GenerateTestData<BenchmarkTelemetry>(actualRecordCount);

            foreach (var item in items)
            {
                t.Insert(item);
            }

            // Benchmark sequential retrieval
            var sw = Stopwatch.StartNew();
            int retrievedCount = 0;

            while (t.Count > 0)
            {
                var item = t.Remove();
                retrievedCount++;
            }

            sw.Stop();

            long elapsedMs = sw.ElapsedMilliseconds;
            double rate = retrievedCount * 1000.0 / elapsedMs;

            LogMessage($"{label} dataset ({retrievedCount} records retrieved):");
            LogMessage($"  Total retrieval time: {elapsedMs} ms");
            LogMessage($"  Retrieval rate: {rate:F2} records/second");
            LogMessage("--------------------------------------");
        }

        private static (double InsertionRate, double RetrievalRate, double BytesPerRecord) RunCustomBenchmark<T>(int recordCount, string label)
            where T : struct
        {
            var db = new Database();
            DropAllTables(db);

            // Create table with fixed max size
            var t = db.CreateTable<T>(MaxTableSize);

            // Generate test data
            var items = GenerateTestData<T>(recordCount);

            // Benchmark insertion with cycling
            var insertSw = Stopwatch.StartNew();

            foreach (var item in items)
            {
                t.Insert(item);

                // Remove oldest when at capacity
                if (t.Count >= MaxTableSize)
                {
                    t.Remove();
                }
            }

            insertSw.Stop();

            long insertMs = insertSw.ElapsedMilliseconds;
            double insertRate = recordCount * 1000.0 / insertMs;

            // Get file size
            string dbFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".talusdb");
            string tablePath = Path.Combine(dbFolder, typeof(T).Name);
            long fileSize = new FileInfo(tablePath).Length;
            double bytesPerRecord = (double)fileSize / Math.Min(recordCount, MaxTableSize);

            // Re-fill table for retrieval test
            DropAllTables(db);
            t = db.CreateTable<T>(MaxTableSize);

            // Only insert up to max size
            var retrievalItemCount = Math.Min(recordCount, MaxTableSize);
            for (int i = 0; i < retrievalItemCount; i++)
            {
                t.Insert(items[i]);
            }

            // Benchmark retrieval
            var retrieveSw = Stopwatch.StartNew();
            int count = 0;

            while (t.Count > 0)
            {
                var item = t.Remove();
                count++;
            }

            retrieveSw.Stop();

            long retrieveMs = retrieveSw.ElapsedMilliseconds;
            double retrieveRate = count * 1000.0 / retrieveMs;

            LogMessage($"{label} dataset ({recordCount} records, max {MaxTableSize} at a time):");
            LogMessage($"  Insertion time: {insertMs} ms, Rate: {insertRate:F2} records/second");
            LogMessage($"  Retrieval time: {retrieveMs} ms, Rate: {retrieveRate:F2} records/second");
            LogMessage($"  Storage per record: {bytesPerRecord:F2} bytes");
            LogMessage("--------------------------------------");

            return (insertRate, retrieveRate, bytesPerRecord);
        }

        private static (long InsertMs, long RetrieveMs, double BytesPerRecord) RunTalusDBBenchmark(int recordCount)
        {
            LogMessage("TALUSDB BENCHMARK:");

            var db = new Database();
            DropAllTables(db);

            // Create table with fixed max size
            var t = db.CreateTable<BenchmarkTelemetry>(MaxTableSize);

            // Benchmark insertion with cycling out old records
            var insertSw = Stopwatch.StartNew();

            for (int i = 0; i < recordCount; i++)
            {
                var item = new BenchmarkTelemetry
                {
                    Timestamp = DateTime.Now.AddMinutes(-i),
                    Value = i * 1.1,
                    SensorId = i % 100,
                    Latitude = 40.0 + (i % 10) * 0.1,
                    Longitude = -74.0 - (i % 10) * 0.1
                };

                t.Insert(item);

                // Remove oldest when at capacity
                if (t.Count >= MaxTableSize)
                {
                    t.Remove();
                }
            }

            insertSw.Stop();

            // Measure file size
            string dbFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".talusdb");
            string tablePath = Path.Combine(dbFolder, typeof(BenchmarkTelemetry).Name);
            var fileInfo = new FileInfo(tablePath);
            long fileSize = fileInfo.Length;
            double bytesPerRecord = (double)fileSize / Math.Min(recordCount, MaxTableSize);

            // Re-create table for retrieval test with exactly MaxTableSize records
            DropAllTables(db);
            t = db.CreateTable<BenchmarkTelemetry>(MaxTableSize);

            // Only fill up to max size
            int fillCount = Math.Min(recordCount, MaxTableSize);
            for (int i = 0; i < fillCount; i++)
            {
                var item = new BenchmarkTelemetry
                {
                    Timestamp = DateTime.Now.AddMinutes(-i),
                    Value = i * 1.1,
                    SensorId = i % 100,
                    Latitude = 40.0 + (i % 10) * 0.1,
                    Longitude = -74.0 - (i % 10) * 0.1
                };
                t.Insert(item);
            }

            // Benchmark retrieval
            var retrieveSw = Stopwatch.StartNew();
            int count = 0;

            while (t.Count > 0)
            {
                var item = t.Remove();
                count++;
            }

            retrieveSw.Stop();

            // Output results
            LogMessage($"  Insertion time: {insertSw.ElapsedMilliseconds} ms");
            LogMessage($"  Insertion rate: {recordCount * 1000 / insertSw.ElapsedMilliseconds:F2} records/second");
            LogMessage($"  Retrieval time: {retrieveSw.ElapsedMilliseconds} ms");
            LogMessage($"  Retrieval rate: {count * 1000 / retrieveSw.ElapsedMilliseconds:F2} records/second");
            LogMessage($"  Storage size: {FormatByteSize(fileSize)}");
            LogMessage($"  Bytes per record: {bytesPerRecord:F2}");

            return (insertSw.ElapsedMilliseconds, retrieveSw.ElapsedMilliseconds, bytesPerRecord);
        }

        private static (long InsertMs, long RetrieveMs, double BytesPerRecord) RunSQLiteBenchmark(int recordCount)
        {
            LogMessage("SQLITE BENCHMARK:");

            // Make sure the database is clean
            if (File.Exists(_sqliteDbPath))
            {
                File.Delete(_sqliteDbPath);
            }

            CreateSQLiteDatabase();

            // Benchmark insertion with cycling
            var insertSw = Stopwatch.StartNew();

            using (var connection = new SQLiteConnection($"Data Source={_sqliteDbPath};Version=3;"))
            {
                connection.Open();

                // Set pragmas for better performance
                using (var pragmaCmd = connection.CreateCommand())
                {
                    pragmaCmd.CommandText = "PRAGMA journal_mode = MEMORY; PRAGMA synchronous = OFF;";
                    pragmaCmd.ExecuteNonQuery();
                }

                var transaction = connection.BeginTransaction();
                int currentRows = 0;

                for (int i = 0; i < recordCount; i++)
                {
                    // Remove oldest record if at capacity
                    if (currentRows >= MaxTableSize)
                    {
                        using (var deleteCmd = connection.CreateCommand())
                        {
                            deleteCmd.Transaction = transaction;
                            deleteCmd.CommandText = "DELETE FROM sensors WHERE rowid = (SELECT MIN(rowid) FROM sensors)";
                            deleteCmd.ExecuteNonQuery();
                        }
                        currentRows--;
                    }

                    // Insert new record
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = "INSERT INTO sensors (timestamp, value, sensor_id, latitude, longitude) VALUES (@timestamp, @value, @sensor_id, @latitude, @longitude)";
                        cmd.Parameters.AddWithValue("@timestamp", DateTime.Now.AddMinutes(-i));
                        cmd.Parameters.AddWithValue("@value", i * 1.1);
                        cmd.Parameters.AddWithValue("@sensor_id", i % 100);
                        cmd.Parameters.AddWithValue("@latitude", 40.0 + (i % 10) * 0.1);
                        cmd.Parameters.AddWithValue("@longitude", -74.0 - (i % 10) * 0.1);
                        cmd.ExecuteNonQuery();
                    }

                    currentRows++;

                    // Commit transaction every 100 records to avoid transaction overhead
                    if (i > 0 && i % 100 == 0)
                    {
                        transaction.Commit();
                        transaction.Dispose();
                        var newTransaction = connection.BeginTransaction();
                        transaction = newTransaction;
                    }
                }

                // Final commit
                transaction.Commit();
            }

            insertSw.Stop();

            // Measure file size
            var fileInfo = new FileInfo(_sqliteDbPath);
            long fileSize = fileInfo.Length;
            double bytesPerRecord = (double)fileSize / Math.Min(recordCount, MaxTableSize);

            // Benchmark retrieval (of exactly MaxTableSize records)
            var retrieveSw = Stopwatch.StartNew();

            using (var connection = new SQLiteConnection($"Data Source={_sqliteDbPath};Version=3;"))
            {
                connection.Open();

                // Retrieve all data in timestamp order
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM sensors ORDER BY timestamp";
                    using (var reader = cmd.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read() && count < MaxTableSize)
                        {
                            var timestamp = reader.GetDateTime(0);
                            var value = reader.GetDouble(1);
                            var sensorId = reader.GetInt32(2);
                            var latitude = reader.GetDouble(3);
                            var longitude = reader.GetDouble(4);
                            count++;
                        }
                    }
                }
            }

            retrieveSw.Stop();

            // Output results
            LogMessage($"  Insertion time: {insertSw.ElapsedMilliseconds} ms");
            LogMessage($"  Insertion rate: {recordCount * 1000 / insertSw.ElapsedMilliseconds:F2} records/second");
            LogMessage($"  Retrieval time: {retrieveSw.ElapsedMilliseconds} ms");
            LogMessage($"  Retrieval rate: {MaxTableSize * 1000 / retrieveSw.ElapsedMilliseconds:F2} records/second");
            LogMessage($"  Storage size: {FormatByteSize(fileSize)}");
            LogMessage($"  Bytes per record: {bytesPerRecord:F2}");

            return (insertSw.ElapsedMilliseconds, retrieveSw.ElapsedMilliseconds, bytesPerRecord);
        }

        private static List<T> GenerateTestData<T>(int count) where T : struct
        {
            var result = new List<T>(count);
            var random = new Random(42); // Fixed seed for reproducibility

            for (int i = 0; i < count; i++)
            {
                if (typeof(T) == typeof(BenchmarkTelemetry))
                {
                    var item = new BenchmarkTelemetry
                    {
                        Timestamp = DateTime.Now.AddMinutes(-i),
                        Value = random.NextDouble() * 100,
                        SensorId = i % 100,
                        Latitude = random.NextDouble() * 180 - 90,
                        Longitude = random.NextDouble() * 360 - 180
                    };
                    result.Add((T)(object)item);
                }
                else if (typeof(T) == typeof(BenchmarkTelemetryWithString))
                {
                    var item = new BenchmarkTelemetryWithString
                    {
                        Timestamp = DateTime.Now.AddMinutes(-i),
                        Value = random.NextDouble() * 100,
                        SensorId = i % 100,
                        SensorName = $"Sensor-{i % 100}-{Guid.NewGuid().ToString().Substring(0, 8)}"
                    };
                    result.Add((T)(object)item);
                }
                else
                {
                    throw new NotSupportedException($"Unsupported type: {typeof(T).Name}");
                }
            }

            return result;
        }

        private static void CreateSQLiteDatabase()
        {
            if (File.Exists(_sqliteDbPath))
            {
                File.Delete(_sqliteDbPath);
            }

            SQLiteConnection.CreateFile(_sqliteDbPath);

            using (var connection = new SQLiteConnection($"Data Source={_sqliteDbPath};Version=3;"))
            {
                connection.Open();

                // Create tables
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE sensors (
                            timestamp DATETIME,
                            value REAL,
                            sensor_id INTEGER,
                            latitude REAL,
                            longitude REAL
                        )";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE sensors_with_string (
                            timestamp DATETIME,
                            value REAL,
                            sensor_id INTEGER,
                            sensor_name TEXT
                        )";
                    cmd.ExecuteNonQuery();
                }

                // Add index on timestamp for efficient ordering
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "CREATE INDEX idx_sensors_timestamp ON sensors(timestamp)";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void DropAllTables(Database db)
        {
            var names = db.GetTableNames();
            foreach (var name in names)
            {
                db.DropTable(name);
            }
        }

        private static string FormatByteSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:F2} {sizes[order]} ({bytes:N0} bytes)";
        }

        private static void LogMessage(string message)
        {
            Console.WriteLine(message);
            File.AppendAllText(_resultsFilePath, message + Environment.NewLine);
        }
    }
}