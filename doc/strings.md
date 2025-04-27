# TalusDB String Support Documentation

## String Support Overview

TalusDB now supports string fields in struct records using the MarshalAs attribute. This allows you to store and retrieve string data in your time-series database. However, there are some important limitations and best practices to be aware of.

## Implementation Details

Strings are supported through the use of the `MarshalAs` attribute with `UnmanagedType.ByValTStr`:

```csharp
[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
public string Name;
```

The implementation uses `Marshal.StructureToPtr` and `Marshal.PtrToStructure` for serialization and deserialization of structs with string fields.

## Limitations

### Fixed-Length Strings

1. **Null Termination**: Strings are stored as null-terminated character arrays. This means the effective maximum length is `SizeConst - 1`. For example, a field defined with `SizeConst = 50` can only store 49 usable characters.

2. **Truncation**: Strings longer than the usable length will be silently truncated to fit.

### Character Support

1. **ASCII and Basic Latin Characters**: Full support for ASCII characters and basic Latin characters.

2. **Extended Latin Characters**: Most Western European characters (é, ü, ñ, etc.) are supported but may have issues in some edge cases.

3. **Limited Unicode Support**: 
   - Multi-byte Unicode characters (surrogate pairs) are not reliably supported
   - Non-Latin scripts (Cyrillic, CJK, Arabic, etc.) are not reliably supported
   - Emoji characters are not supported as they use surrogate pairs
   - Combining characters may not work as expected
   - Bidirectional text control may be lost

### Storage Efficiency

Fixed-length strings always consume their maximum allocated space, even when storing short strings. This can lead to storage inefficiency if your string fields have large size constants but typically store short values.

## Best Practices

1. **Size Constants**: 
   - Choose `SizeConst` values carefully, remembering that one character is reserved for the null terminator
   - Add a small buffer (5-10%) to account for occasional longer strings
   - Set `SizeConst = X + 1` if you need to store exactly X characters

2. **String Handling**:
   - Always use `TrimEnd('\0')` when retrieving strings for comparison or display
   - Check string lengths before inserting to avoid unexpected truncation
   - Avoid special characters, non-Latin scripts, and emojis when possible

3. **Testing**:
   - Test boundary conditions (empty strings, strings at maximum length, strings that exceed the limit)
   - Verify the behavior of your application with truncated strings

4. **Alternative for Complex Text**:
   - For full Unicode support, consider using the `JsonTable<T>` implementation instead
   - JsonTable has better support for complex string data but may have lower performance

## Example Usage

```csharp
// Define a struct with a 50-character field (49 usable characters)
public struct SensorReading
{
    public DateTime Timestamp;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
    public string Location;
    public double Value;
}

// Creating and retrieving
var reading = new SensorReading
{
    Timestamp = DateTime.Now,
    Location = "Building A, Room 123", // Well under the 49-char limit
    Value = 72.5
};

table.Insert(reading);
var result = table.Remove();

// Note the TrimEnd('\0') to handle null terminator
Console.WriteLine($"Location: {result.Value.Location.TrimEnd('\0')}");
```

By following these guidelines, you can effectively use string fields in your TalusDB records while avoiding common pitfalls.