# TalusDB vs SQLite Resource-Constrained Benchmark

This console application provides comprehensive benchmarks comparing TalusDB and SQLite for time-series data storage on embedded systems with limited resources.

## Resource-Constrained Simulation

This benchmark simulates a resource-constrained environment (like an STM32 microcontroller) by:

1. **Limiting table size** to 1,000 records maximum
2. **Cycling out old records** when the limit is reached
3. **Measuring real-world performance** with continuous data cycling

## Benchmark Results

### High-End Desktop Results (Example)

```
=== LARGE DATASET COMPARISON (100000 RECORDS) ===
Max table size: 1000 records
TALUSDB BENCHMARK:
  Insertion time: 7093 ms
  Insertion rate: 14098.00 records/second
  Retrieval time: 53 ms
  Retrieval rate: 18867.00 records/second
  Storage size: 39.09 KB (40,032 bytes)
  Bytes per record: 40.03
SQLITE BENCHMARK:
  Insertion time: 2449 ms
  Insertion rate: 40832.00 records/second
  Retrieval time: 7 ms
  Retrieval rate: 142857.00 records/second
  Storage size: 144.00 KB (147,456 bytes)
  Bytes per record: 147.46
=== COMPARISON RESULTS ===
Insertion Performance:
  SQLite is 2.90x faster than TalusDB
Retrieval Performance:
  SQLite is 7.57x faster than TalusDB
Storage Efficiency:
  TalusDB is 3.68x more storage efficient than SQLite
```

### Results Analysis

These results from a high-end desktop demonstrate some key differences between TalusDB and SQLite:

1. **Performance Characteristics**:
   - SQLite shows superior raw performance on powerful hardware (2.9x faster insertions, 7.57x faster retrievals)
   - TalusDB offers significantly better storage efficiency (3.68x less storage per record)

2. **Why These Results Matter for Embedded Systems**:
   - On powerful desktop hardware, SQLite's optimized query engine shows its strength
   - However, on resource-constrained embedded systems like STM32, the storage efficiency and smaller code footprint of TalusDB become more important
   - While SQLite is faster on desktop-class hardware, this gap would likely narrow on embedded systems due to I/O limitations and memory constraints

3. **Key Takeaways**:
   - For embedded systems with limited storage, TalusDB's 3.68x storage efficiency is a significant advantage
   - The total storage size for TalusDB (39KB) vs SQLite (144KB) represents important savings on devices with limited flash memory
   - SQLite's higher performance comes at the cost of more complex code and larger resource requirements

4. **Expected Performance on STM32**:
   - On STM32, the performance gap would likely narrow considerably due to I/O bottlenecks
   - Memory usage becomes more critical - TalusDB's lower memory requirements would be a significant advantage
   - The 3.68x storage efficiency means TalusDB could store 3.68x more data in the same amount of flash memory

## Requirements

- .NET Core 3.1 or later
- WildernessLabs.TalusDB library
- System.Data.SQLite.Core package

## Setup

1. Update the `.csproj` file to point to your TalusDB library location:

```xml
<Reference Include="WildernessLabs.TalusDB">
  <HintPath>path/to/WildernessLabs.TalusDB.dll</HintPath>
</Reference>
```

2. Build the project:

```
dotnet build
```

## Running the Benchmarks

Run the application from the command line:

```
dotnet run
```

The application will present a menu of benchmark options:

1. Insertion Performance (TalusDB only)
2. Retrieval Performance (TalusDB only)
3. String vs Numeric Performance (TalusDB only)
4. TalusDB vs SQLite - Small Dataset (1,000 records)
5. TalusDB vs SQLite - Medium Dataset (10,000 records)
6. TalusDB vs SQLite - Large Dataset (100,000 records)
7. Storage Efficiency Comparison
8. Run All Benchmarks
9. Test TalusDB Stream Behavior (AlwaysNew vs KeepOpen)

## Optimizations Tested

### TalusDB Optimizations

Option 9 tests the impact of changing TalusDB's stream behavior from the default `AlwaysNew` to `KeepOpen`, which can significantly improve insertion performance by reducing file I/O overhead.

For further performance improvements, consider:
- Implementing batch insertion methods
- Using memory-mapped files
- Adding write buffering

### SQLite Optimizations

The SQLite tests include performance optimizations:
- `PRAGMA journal_mode = MEMORY` to use in-memory journaling
- `PRAGMA synchronous = OFF` to reduce fsync calls
- Batch transactions (commit every 100 records)
- Index on timestamp for efficient retrieval

## Benchmark Results

Results are displayed in the console and also saved to `benchmark_results.txt` in the application directory for future reference.

## Metrics Measured

1. **Insertion Performance**: Records per second for sequential insertion
2. **Retrieval Performance**: Records per second for sequential retrieval
3. **Storage Efficiency**: Bytes per record on disk
4. **String Handling**: Overhead of string fields vs. numeric-only records
5. **Stream Behavior Impact**: Performance difference between file access modes

## Understanding the Results

- **Insertion Rate**: Higher is better. Measures how quickly each database can store new records while managing the fixed table size limit.
- **Retrieval Rate**: Higher is better. Measures how quickly each database can read back stored records.
- **Bytes per Record**: Lower is better. Measures storage efficiency.

## Modifying the Benchmark Parameters

You can easily modify the benchmark parameters by changing the constants at the top of the `Program.cs` file:

```csharp
private const int SmallDataset = 1_000;
private const int MediumDataset = 10_000;
private const int LargeDataset = 100_000;
private const int MaxTableSize = 1_000;
```