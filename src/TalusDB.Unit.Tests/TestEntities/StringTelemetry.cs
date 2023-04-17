using System.Runtime.InteropServices;

namespace TalusDB.Unit.Tests.TestEntities
{
    public struct InvalidStringFieldTelemetry
    {
        public DateTime Timestamp;
        // note: no MarshalAs, so it's not a fixed-size
        public string Name;
    }

    public struct InvalidStringPropertyTelemetry
    {
        public DateTime Timestamp { get; set; }
        public string Name { get; set; }
    }

    public struct InvalidPropertyTypeTelemetry
    {
        public class Foo
        {
            public string Name { get; set; }
        }

        public DateTime Timestamp { get; set; }
        public Foo bar { get; set; }
    }

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