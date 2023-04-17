using System;
using System.IO;
using System.Runtime.InteropServices;

namespace WildernessLabs.TalusDB
{
    public enum StreamBehavior
    {
        AlwaysNew,
        KeepOpen
    }

    public class Table<T> where T : struct
    {
        private const int HeaderSize = 32; // currently only 16 used - leaving space for future additions

        private FileInfo _fileInfo;
        private FileStream _stream;
        private int _stride;
        private bool _midRangeReached = false;
        private object _syncRoot = new object();
        private StreamBehavior _streamBehavior;

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
        public bool HighWaterExceeded { get; private set; } = false;
        public bool LowWaterExceeded { get; private set; } = false;

        public StreamBehavior StreamBehavior { get; private set; }

        internal Table(string rootFolder, int maxRecords)
        {
            // each table is a file in the database folder

            // TODO: add ability to change this
            StreamBehavior = StreamBehavior.AlwaysNew;

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
            get => ReadHeader(0);
            set => WriteHeader(0, value);
        }

        private int Tail
        {
            get => ReadHeader(4);
            set => WriteHeader(4, value);
        }

        private int Stride
        {
            get
            {
                if (_stride == 0)
                {
                    _stride = ReadHeader(8);
                }

                return _stride;
            }
            set
            {
                if (_stride == 0)
                {
                    WriteHeader(8, value);
                }
                _stride = value;
            }
        }

        /// <summary>
        /// Gets the maximum number of elements the table can hold.
        /// </summary>
        public int MaxElements
        {
            get => ReadHeader(12);
            private set => WriteHeader(12, value);
        }

        private void IncrementTail()
        {
            Tail += Stride;
            if (Tail >= (MaxElements * Stride))
            {
                Tail = HeaderSize;
            }
        }

        private void IncrementHead()
        {
            Head += Stride;
            if (Head >= (MaxElements * Stride))
            {
                Head = 0;
            }

            if (Head == Tail)
            {
                IsFull = true;
            }
        }

        public void Truncate()
        {
            lock (_syncRoot)
            {
                var stream = GetStream();
                stream.SetLength(HeaderSize);
                FinishedWithStream(stream);
                Head = 0;
                Tail = 0;

                HighWaterExceeded = false;
                LowWaterExceeded = true;
                _midRangeReached = false;
                IsFull = false;
                HasOverrun = false;
                HasUnderrun = false;
            }
        }

        public int Count
        {
            get
            {
                lock (_syncRoot)
                {
                    if (IsFull) return MaxElements;

                    if (Head == Tail) return 0;

                    // special case for head at the "end" (which is also the beginning)
                    if (Head == 0)
                    {
                        return MaxElements - (Tail / Stride);
                    }

                    if (Head > Tail)
                    {
                        return (Head - Tail) / Stride;
                    }

                    return MaxElements - ((Tail + Head) / Stride);
                }
            }
        }

        public void Insert(T item)
        {
            lock (_syncRoot)
            {
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
                stream.Seek(head + HeaderSize, SeekOrigin.Begin);

                var sourceItem = MemoryMarshal.CreateSpan<T>(ref item, 1);
                var sourceBytes = MemoryMarshal.Cast<T, byte>(sourceItem);
                stream.Write(sourceBytes);
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
        /// Removes one element from the tail of the buffer, if one exists
        /// </summary>
        /// <returns></returns>
        public T? Select()
        {
            return GetOldest(true);
        }

        /// <summary>
        /// Returns the element currently at the tail of the buffer, if one exists, without removing it
        /// </summary>
        /// <returns></returns>
        public T? Peek()
        {
            return GetOldest(false);
        }

        private T? GetOldest(bool remove)
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
                stream.Seek(tail + HeaderSize, SeekOrigin.Begin);
                Span<byte> buffer = stackalloc byte[Stride];
                stream.Read(buffer);
                FinishedWithStream(stream);

                var sourceItem = MemoryMarshal.Cast<byte, T>(buffer);

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

                return sourceItem[0];
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
    }
}
