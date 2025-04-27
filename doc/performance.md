# TalusDB vs SQLite: Performance Comparison Guide

This guide provides a comprehensive comparison between TalusDB and SQLite for time-series data storage on embedded systems. It's intended to help you decide which database solution is better suited for your specific needs.

## Key Differences at a Glance

| Feature | TalusDB | SQLite |
|---------|---------|--------|
| **Architecture** | Circular buffer, FIFO | Relational database |
| **Optimized for** | Sequential time-series data | Indexed random access |
| **Query capabilities** | Limited (FIFO access only) | Extensive (SQL) |
| **Memory footprint** | Very small (~250-400KB smaller than SQLite) | Moderate (~400KB minimum) |
| **Dependencies** | Minimal | Self-contained |
| **String support** | Fixed-length with limitations | Full Unicode support |
| **Persistence** | Single file per table | Single file for entire database |

## Performance Characteristics

### Insertion Performance

TalusDB typically offers **faster insertion rates** than SQLite, especially for sequential time-series data. This is because:

1. TalusDB uses a simple append operation with no indexing overhead
2. SQLite must maintain indices and enforce constraints during insertion
3. TalusDB's fixed-record structure eliminates fragmentation issues

### Retrieval Performance

The retrieval performance comparison depends on access patterns:

1. **Sequential access**: TalusDB is typically faster for simple FIFO retrieval
2. **Random access**: SQLite excels with indexed queries while TalusDB doesn't support random access
3. **Filtering data**: SQLite can filter with WHERE clauses; TalusDB requires manual scanning

### Storage Efficiency

Storage efficiency varies based on data structure:

1. **Simple numeric data**: TalusDB is typically more storage-efficient
2. **String data**: SQLite may be more efficient due to variable-length storage
3. **Fixed-size records**: TalusDB has no overhead for variable-length fields

### Memory Usage

TalusDB has a significantly smaller memory footprint:

1. TalusDB runtime typically requires <100KB RAM
2. SQLite requires ~400KB minimum RAM
3. This difference can be critical on memory-constrained MCUs like STM32

## When to Choose Each Option

### Choose TalusDB when:

- Your application involves sequential time-series data (logs, sensor readings, etc.)
- You're working with severely memory-constrained devices (STM32, etc.)
- You need extremely fast insertion for high-frequency data logging
- Your data retrieval pattern is primarily FIFO (oldest-first)
- You can work within the limitations of fixed-size records
- You're primarily storing numeric data with limited string usage

### Choose SQLite when:

- You need complex queries and filtering capabilities
- Your application requires indexed random access
- You need to store variable-length text data with full Unicode support
- You're working with relational data that benefits from SQL joins
- You require ACID transaction guarantees
- Memory constraints are not a primary concern

## Implementation Recommendations

### For TalusDB:

1. Use structs with fixed-size fields
2. Be mindful of string limitations (null termination, character support)
3. Design data structures to minimize wasted space in fixed-size records
4. Consider using JsonTable for complex text data

### For SQLite:

1. Create appropriate indices for your query patterns
2. Use transactions for batch operations
3. Configure SQLite cache size appropriately for your memory constraints
4. Use prepared statements to optimize repeated queries

## Benchmark Methodology

The benchmark tests focus on these key metrics:

1. **Insertion rate**: Records per second for sequential insertion
2. **Retrieval rate**: Records per second for sequential retrieval
3. **Storage efficiency**: Bytes per record on disk
4. **Memory usage**: RAM requirements during operation

The benchmarks use identical data structures and content to ensure fair comparison.

## Conclusion

TalusDB and SQLite serve different niches in the embedded database space. TalusDB offers superior performance for time-series data on highly constrained systems, while SQLite provides more flexibility and query capabilities at the cost of higher resource usage. 

Your choice should be guided by your specific application requirements, memory constraints, and data access patterns.
