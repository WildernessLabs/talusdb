using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace WildernessLabs.TalusDB;

/// <summary>
/// A TalusDB storage file for telemetry data
/// </summary>
/// <typeparam name="T"></typeparam>
public class Table<T> : ITable<T>, IDisposable
    where T : struct
{
    internal event EventHandler PublicationStateChanged = delegate { };

    private const int HeaderSize = 32; // currently only 16 used - leaving space for future additions
    private const int HeadHeaderOffset = 0;
    private const int TailHeaderOffset = 4;
    private const int StrideHeaderOffset = 8;
    private const int MaxElementsHeaderOffset = 12;

    private readonly FileInfo _fileInfo;
    private readonly object _syncRoot = new object();

    private FileStream? _stream = null;
    private int _stride;
    private bool _midRangeReached = false;
    private readonly bool _useMemoryMarshal = true;
    private bool _publicationEnabled = false;
    private bool _containsMarshaledStrings = false;
    private int _totalFixedStringSize = 0;

    /// <summary>
    /// Fires when an element is added to the Table
    /// </summary>
    public event EventHandler ItemAdded = delegate { };
    /// <summary>
    /// Fires when an element is added to a Table when it is already full
    /// </summary>
    public event EventHandler Overrun = delegate { };
    /// <summary>
    /// Fires when an attempt is made to remove an item from an empty Table
    /// </summary>
    public event EventHandler Underrun = delegate { };
    /// <summary>
    /// Fires when the number of elements in a Table reaches a non-zero HighWaterLevel value on an Enqueue call.  This event fires only once when passing upward across the boundary.
    /// </summary>
    public event EventHandler HighWater = delegate { };
    /// <summary>
    /// Fires when the number of elements in a table reaches a non-zero LowWaterLevel value on a Remove call.  This event fires only once when passing downward across the boundary.
    /// </summary>
    public event EventHandler LowWater = delegate { };

    /// <summary>
    /// When set to <b>true</b>, overrun conditions will throw a TalusException.  Default is <b>false</b>.
    /// </summary>
    public bool ExceptOnOverrun { get; set; }
    /// <summary>
    /// When set to <b>true</b>, underrun conditions will throw a TalusException.  Default is <b>false</b>.
    /// </summary>
    public bool ExceptOnUnderrun { get; set; }
    /// <summary>
    /// Returns true when an overrun condition has occurred.
    /// </summary>
    /// <remarks>
    /// The Table will never reset this value except when Truncate is called.  It is up to the consumer to set this back to false if desired.
    /// </remarks>
    public bool HasOverrun { get; set; }
    /// <summary>
    /// Returns true when an underrun condition has occurred.
    /// </summary>
    /// <remarks>
    /// The table will never reset this value except when Truncate is called.  It is up to the consumer to set this back to false if desired.
    /// </remarks>
    public bool HasUnderrun { get; set; }
    /// <summary>
    /// Returns <b>true</b> if the Table's Count equals its MaxElements.
    /// </summary>
    public bool IsFull { get; private set; }
    /// <summary>
    /// The HighWater event will fire when the Table contains this many (or more) elements.
    /// </summary>
    /// <remarks>
    /// Set the value to zero (default) to disable high-water notifications
    /// </remarks>
    public int HighWaterLevel { get; set; }
    /// <summary>
    /// The LowWater event will fire when the Table contains this many (or less) elements.
    /// </summary>
    /// <remarks>
    /// Set the value to zero (default) to disable low-water notifications
    /// </remarks>
    public int LowWaterLevel { get; set; }
    /// <summary>
    /// Gets a flag that indicates whether the HighWaterLevel has been exceeded
    /// </summary>
    public bool HighWaterExceeded { get; private set; } = false;
    /// <summary>
    /// Gets a flag that indicates whether the LowWaterLevel has been exceeded
    /// </summary>
    public bool LowWaterExceeded { get; private set; } = false;
    /// <summary>
    /// Gets the Table's StreamBehavior
    /// </summary>
    public StreamBehavior StreamBehavior { get; private set; }
    /// <summary>
    /// Gets the Table's Disposed state
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// When true, will be published through the Database Publisher
    /// </summary>
    public bool PublicationEnabled
    {
        get => _publicationEnabled;
        set
        {
            if (value == PublicationEnabled) return;
            _publicationEnabled = value;
            PublicationStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal Table(string rootFolder, int maxRecords, StreamBehavior streamBehavior = StreamBehavior.KeepOpen)
    {
        // each table is a file in the database folder

        StreamBehavior = streamBehavior;

        _fileInfo = new FileInfo(Path.Combine(rootFolder, typeof(T).Name));

        if (!_fileInfo.Exists)
        {
            if (maxRecords <= 0) throw new ArgumentException(nameof(maxRecords));

            var stream = GetStream();

            // set header info
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(new byte[HeaderSize]);
            FinishedWithStream(stream);
            Head = 0;
            Tail = 0;
            Stride = Marshal.SizeOf(typeof(T));
            MaxElements = maxRecords;
        }
        else
        {
            // Read the existing values
            Stride = ReadHeader(StrideHeaderOffset);
            MaxElements = ReadHeader(MaxElementsHeaderOffset);
            // Head and Tail are read when needed via properties
        }

        // Check for strings first
        CheckForMarshaledStrings();

        // Only try memory marshaling if no strings were found
        if (!_containsMarshaledStrings)
        {
            try
            {
                T instance = (T)FormatterServices.GetUninitializedObject(typeof(T));
                MemoryMarshal.AsBytes<T>(new T[] { instance });
                // type is blittable
                _useMemoryMarshal = true;
            }
            catch
            {
                _useMemoryMarshal = false;
            }
        }
        else
        {
            // If we have marshaled strings, we must use the Marshal approach
            _useMemoryMarshal = false;
        }
    }

    private void CheckForMarshaledStrings()
    {
        // Check properties
        foreach (var prop in typeof(T).GetProperties())
        {
            if (!prop.PropertyType.IsValueType)
            {
                throw new TalusException($"Type '{typeof(T).Name}' is not blittable due to Property '{prop.Name}'");
            }
        }

        // Check fields
        foreach (var field in typeof(T).GetFields())
        {
            if (!field.FieldType.IsValueType)
            {
                if (field.FieldType.Equals(typeof(string)))
                {
                    var marshalAttrs = field.GetCustomAttributes(typeof(MarshalAsAttribute), true);
                    if (marshalAttrs.Any())
                    {
                        _containsMarshaledStrings = true;
                        var marshalAttr = (MarshalAsAttribute)marshalAttrs.First();
                        if (marshalAttr.Value == UnmanagedType.ByValTStr)
                        {
                            _totalFixedStringSize += marshalAttr.SizeConst;
                        }
                    }
                    else
                    {
                        throw new TalusException($"Type '{typeof(T).Name}' is not blittable due to Field '{field.Name}' not using MarshalAs");
                    }
                }
                else
                {
                    throw new TalusException($"Type '{typeof(T).Name}' is not blittable due to Field '{field.Name}'");
                }
            }
        }

        if (_containsMarshaledStrings)
        {
            // Ensure the stride is at least as large as needed for the fixed strings
            int minimumStride = Marshal.SizeOf(typeof(T));
            if (Stride < minimumStride)
            {
                Stride = minimumStride;
            }
        }
    }

    private FileStream GetStream()
    {
        _fileInfo.Refresh();

        switch (StreamBehavior)
        {
            case StreamBehavior.AlwaysNew:
                if (!_fileInfo.Exists)
                {
                    return _fileInfo.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                }
                return _fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            default:
                if (_stream == null)
                {
                    if (!_fileInfo.Exists)
                    {
                        _stream = _fileInfo.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                    }
                    else
                    {
                        _stream = _fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    }
                }
                return _stream;
        }
    }

    private void FinishedWithStream(FileStream stream)
    {
        switch (StreamBehavior)
        {
            case StreamBehavior.AlwaysNew:
                stream.Flush();
                stream.Close();
                stream.Dispose();
                break;
            default:
                stream.Flush();
                break;
        }
    }

    internal static Table<T> Open(string rootFolder)
    {
        var fi = new FileInfo(Path.Combine(rootFolder, typeof(T).Name));

        if (!fi.Exists)
        {
            throw new TalusException("Table not found");
        }

        return new Table<T>(rootFolder, -1);
    }

    private int ReadHeader(int offset)
    {
        lock (_syncRoot)
        {
            Span<byte> b = stackalloc byte[4];
            var stream = GetStream();
            try
            {
                stream.Seek(offset, SeekOrigin.Begin);
                stream.Read(b);
                return MemoryMarshal.Read<int>(b);
            }
            finally
            {
                FinishedWithStream(stream);
            }
        }
    }

    private void WriteHeader(int offset, int value)
    {
        lock (_syncRoot)
        {
            var b = BitConverter.GetBytes(value);
            var stream = GetStream();
            try
            {
                stream.Seek(offset, SeekOrigin.Begin);
                stream.Write(b);
            }
            finally
            {
                FinishedWithStream(stream);
            }
        }
    }

    private int Head
    {
        get => ReadHeader(HeadHeaderOffset);
        set => WriteHeader(HeadHeaderOffset, value);
    }

    private int Tail
    {
        get => ReadHeader(TailHeaderOffset);
        set => WriteHeader(TailHeaderOffset, value);
    }

    private int Stride
    {
        get
        {
            if (_stride == 0)
            {
                _stride = ReadHeader(StrideHeaderOffset);
            }

            return _stride;
        }
        set
        {
            if (_stride == 0)
            {
                WriteHeader(StrideHeaderOffset, value);
            }
            _stride = value;
        }
    }

    /// <summary>
    /// Gets the maximum number of elements the table can hold.
    /// </summary>
    public int MaxElements
    {
        get => ReadHeader(MaxElementsHeaderOffset);
        private set => WriteHeader(MaxElementsHeaderOffset, value);
    }

    private void IncrementTail()
    {
        var newTail = Tail + Stride;
        // If we've reached the end of the buffer, wrap around to HeaderSize
        if (newTail >= (HeaderSize + MaxElements * Stride))
        {
            newTail = HeaderSize;
        }
        Tail = newTail;
    }

    private void IncrementHead()
    {
        var newHead = Head + Stride;
        // If we've reached the end of the buffer, wrap around to 0
        if (newHead >= (HeaderSize + MaxElements * Stride))
        {
            newHead = HeaderSize;
        }
        Head = newHead;

        // If head catches up to tail, buffer is full
        if (Head == Tail)
        {
            IsFull = true;
        }
    }

    /// <summary>
    /// Truncates (removes all data from) the Table
    /// </summary>
    public void Truncate()
    {
        lock (_syncRoot)
        {
            var stream = GetStream();
            // Just reset to header size to clear all data
            stream.SetLength(HeaderSize);
            FinishedWithStream(stream);

            // Reset pointers
            Head = HeaderSize;
            Tail = HeaderSize;

            // Reset state flags
            HighWaterExceeded = false;
            LowWaterExceeded = true;
            _midRangeReached = false;
            IsFull = false;
            HasOverrun = false;
            HasUnderrun = false;
        }
    }

    /// <summary>
    /// Gets the current number of valid Elements stored in the Table
    /// </summary>
    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                if (IsFull) return MaxElements;

                var head = Head;
                var tail = Tail;

                if (head == tail) return 0;

                // Calculate element count based on head and tail positions
                if (head > tail)
                {
                    return (head - tail) / Stride;
                }
                else
                {
                    // Head has wrapped around
                    var endSize = (HeaderSize + MaxElements * Stride) - tail;
                    var startSize = head - HeaderSize;
                    return (endSize + startSize) / Stride;
                }
            }
        }
    }

    /// <summary>
    /// Inserts an Element into the table
    /// </summary>
    /// <param name="element"></param>
    public void Insert(T element)
    {
        lock (_syncRoot)
        {
            // Initialize head and tail if they're at zero (new table)
            if (Head == 0 && Tail == 0)
            {
                Head = HeaderSize;
                Tail = HeaderSize;
            }

            if (IsFull)
            {
                // drop the tail item
                IncrementTail();

                // notify the consumer
                OnOverrun();
            }

            // put the new item in the list
            var head = Head;
            var stream = GetStream();
            stream.Seek(head, SeekOrigin.Begin);

            try
            {
                if (_useMemoryMarshal && !_containsMarshaledStrings)
                {
                    var sourceItem = MemoryMarshal.CreateSpan<T>(ref element, 1);
                    var sourceBytes = MemoryMarshal.Cast<T, byte>(sourceItem);
                    stream.Write(sourceBytes);
                }
                else
                {
                    var size = Marshal.SizeOf(element);
                    var data = new byte[size];

                    // Create unmanaged memory
                    var ptr = Marshal.AllocHGlobal(size);
                    try
                    {
                        // Copy the struct to unmanaged memory
                        Marshal.StructureToPtr(element, ptr, false);

                        // Copy the unmanaged memory to our byte array
                        Marshal.Copy(ptr, data, 0, size);

                        // Write the data to the stream
                        stream.Write(data);
                    }
                    finally
                    {
                        // Always free the unmanaged memory
                        Marshal.FreeHGlobal(ptr);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new TalusException($"Error inserting element: {ex.Message}");
            }

            FinishedWithStream(stream);

            IncrementHead();

            if ((HighWaterLevel > 0) && (Count >= HighWaterLevel))
            {
                if (!HighWaterExceeded)
                {
                    HighWaterExceeded = true;
                    HighWater?.Invoke(this, EventArgs.Empty);
                }
            }

            if (LowWaterLevel > 0)
            {
                if (Count > LowWaterLevel)
                {
                    _midRangeReached = true;
                    LowWaterExceeded = false;
                }
            }

            // do notifications
            ItemAdded?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Removes one element from the tail of the table, if one exists
    /// </summary>
    /// <returns></returns>
    public T? Remove()
    {
        return GetOldest(true);
    }

    /// <summary>
    /// Returns the element currently at the tail of the Table, if one exists, without removing it
    /// </summary>
    /// <returns></returns>
    T ITable<T>.Peek2()
    {
        return GetOldest(false);
    }

    private T GetOldest(bool remove)
    {
        lock (_syncRoot)
        {
            if ((Count == 0) && !(IsFull))
            {
                OnUnderrun();
                return default;
            }

            var tail = Tail;
            var stream = GetStream();
            stream.Seek(tail, SeekOrigin.Begin);

            // Read the exact stride size
            Span<byte> buffer = stackalloc byte[Stride];
            var bytesRead = stream.Read(buffer);
            FinishedWithStream(stream);

            // Verify we read something
            if (bytesRead == 0)
            {
                throw new TalusException("Read 0 bytes when retrieving element");
            }

            T sourceItem;
            try
            {
                if (_useMemoryMarshal && !_containsMarshaledStrings)
                {
                    sourceItem = MemoryMarshal.Cast<byte, T>(buffer)[0];
                }
                else
                {
                    var size = Marshal.SizeOf(typeof(T));

                    // Allocate unmanaged memory
                    var ptr = Marshal.AllocHGlobal(size);
                    try
                    {
                        // Copy from our byte array to unmanaged memory
                        Marshal.Copy(buffer.ToArray(), 0, ptr, size);

                        // Convert the unmanaged memory to our struct
                        sourceItem = Marshal.PtrToStructure<T>(ptr);
                    }
                    finally
                    {
                        // Always free the unmanaged memory
                        Marshal.FreeHGlobal(ptr);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new TalusException($"Error reading element: {ex.Message}");
            }

            if (remove)
            {
                IncrementTail();
                IsFull = false;

                if ((HighWaterLevel > 0) && (Count < HighWaterLevel))
                {
                    HighWaterExceeded = false;
                }

                if ((LowWaterLevel > 0) && (Count <= LowWaterLevel))
                {
                    // only raise as we pass from above to below the low water line
                    if (_midRangeReached)
                    {
                        LowWaterExceeded = true;
                        _midRangeReached = false;
                        LowWater?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            return sourceItem;
        }
    }

    private void OnOverrun()
    {
        HasOverrun = true;

        if (ExceptOnOverrun)
        {
            throw new TalusException("Overrun");
        }
        Overrun?.Invoke(this, EventArgs.Empty);
    }

    private void OnUnderrun()
    {
        HasUnderrun = true;

        if (ExceptOnUnderrun)
        {
            throw new TalusException("Underrun");
        }
        Underrun?.Invoke(this, EventArgs.Empty);
    }

    T ITable<T>.Remove()
    {
        var result = Remove();
        if (!result.HasValue)
        {
            throw new TalusException("Cannot remove from empty table");
        }
        return result.Value;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                _stream?.Close();
                _stream?.Dispose();
            }

            IsDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public T? Peek()
    {
        throw new NotImplementedException();
    }
}