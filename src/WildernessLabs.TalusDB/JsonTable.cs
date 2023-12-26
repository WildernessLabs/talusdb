﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace WildernessLabs.TalusDB;

public class JsonTable<T> : ITable<T>
    where T : new()
{
    public bool PublicationEnabled => throw new NotImplementedException();

    public event EventHandler ItemAdded;
    public event EventHandler Overrun;
    public event EventHandler Underrun;
    public event EventHandler HighWater;
    public event EventHandler LowWater;

    /// <summary>
    /// Gets the Table's StreamBehavior
    /// </summary>
    public StreamBehavior StreamBehavior { get; private set; }

    private const int BlockSize = 16;
    private const int HeaderSize = 32;
    private const int HeadHeaderOffset = 0;
    private const int TailHeaderOffset = 4;
    private const int CountHeaderOffset = 8;
    private const int MaxBlocksHeaderOffset = 12;

    private FileStream? _stream = null;
    private readonly FileInfo _fileInfo;
    private object _syncRoot = new();

    public bool IsFull { get; private set; }

    internal JsonTable(string rootFolder, int maxBlocks)
    {
        StreamBehavior = StreamBehavior.AlwaysNew;

        _fileInfo = new FileInfo(Path.Combine(rootFolder, typeof(T).Name));

        if (!_fileInfo.Exists)
        {
            if (maxBlocks <= 0) throw new ArgumentException(nameof(maxBlocks));

            var stream = GetStream();

            // set header info
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(new byte[HeaderSize]);
            FinishedWithStream(stream);
            HeadPointer = 0;
            //            TailPointer = 0;
            //            Count = 0
            //            MaxBlocks = maxBlocks;
        }
    }

    public int Count
    {
        get => ReadHeaderValue(CountHeaderOffset);
        private set => WriteHeaderValue(CountHeaderOffset, value);
    }

    public void Insert(T element)
    {
        var blockString = JsonSerializer.Serialize(element);
        var block = Encoding.UTF8.GetBytes(blockString);
        var padding = new byte[BlockSize - ((block.Length + 1) % BlockSize)];
        var blocksRequired = (block.Length + padding.Length + 1) / BlockSize;

        lock (_syncRoot)
        {
            var head = HeadPointer;
            var stream = GetStream();
            stream.Seek(head + HeaderSize, SeekOrigin.Begin);

            stream.WriteByte(0xff);
            stream.Write(block);
            stream.Write(padding);

            FinishedWithStream(stream);

            Count++;
            IncrementHead(blocksRequired);
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

    private int HeadPointer
    {
        get => ReadHeaderValue(HeadHeaderOffset);
        set => WriteHeaderValue(HeadHeaderOffset, value);
    }

    private int TailPointer
    {
        get => ReadHeaderValue(TailHeaderOffset);
        set => WriteHeaderValue(TailHeaderOffset, value);
    }

    private int ReadHeaderValue(int offset)
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

    private void WriteHeaderValue(int offset, int value)
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

    private void IncrementHead(int blocks)
    {
        lock (_syncRoot)
        {
            HeadPointer += (blocks * BlockSize);
        }
    }

    public T? Remove()
    {
        return GetOldest(true);
    }

    /// <summary>
    /// Returns the element currently at the tail of the Table, if one exists, without removing it
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
            /*
            if ((Count == 0) && !(IsFull))
            {
                OnUnderrun();
                return default;
            }
            */

            if (Count == 0)
            {
                return default;
            }

            long tail = TailPointer;
            var stream = GetStream();
            var blockStart = tail + HeaderSize;

            // we're always at least 1 block, so skip to the second
            stream.Seek(blockStart + BlockSize, SeekOrigin.Begin);
            // find the next block start = skip by block until we find the start of the next record
            var blocks = 1;
            var b = stream.ReadByte(); // this is the block start indicator
            while (b != 0xff)
            {
                stream.Position += BlockSize - 1; // -1 because we read 1 for the marker

                // are we past the end of the stream?
                if (stream.Position > stream.Length)
                {
                    stream.Position = HeaderSize;
                }

                // have we passed the tail?
                if (stream.Position == tail)
                {
                    break;
                }

                b = stream.ReadByte();
                blocks++;
            }

            Span<byte> buffer = stackalloc byte[(blocks * BlockSize) - 1];
            stream.Seek(blockStart + 1, SeekOrigin.Begin);
            stream.Read(buffer);
            tail = stream.Position;
            stream.Seek(blockStart, SeekOrigin.Begin);
            if (remove)
            {
                stream.WriteByte(0x00); // invalidate start marker
            }
            FinishedWithStream(stream);

            var serialized = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
            var item = JsonSerializer.Deserialize<T>(serialized);

            if (remove)
            {

                TailPointer = (int)tail;
                Count--;
                IsFull = false;

                /*
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
                */
            }

            return item;
        }
    }
}
