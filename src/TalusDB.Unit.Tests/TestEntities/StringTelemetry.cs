﻿using System.Runtime.InteropServices;

namespace TalusDB.Unit.Tests.TestEntities
{
    [StructLayout(LayoutKind.Sequential)]
    public struct StringTelemetry : IEquatable<StringTelemetry>
    {
        public DateTime Timestamp;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Name;

        public bool Equals(StringTelemetry other)
        {
            if (Timestamp.Ticks != other.Timestamp.Ticks) return false;
            if (Name != other.Name) return false;
            return true;
        }
    }
}