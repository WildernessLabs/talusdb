using System.Runtime.InteropServices;

namespace TalusDB.Unit.Tests.TestEntities;

// Valid struct with a string field using MarshalAs
public struct StringTelemetry
{
    public DateTime Timestamp;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
    public string Name;

    public override bool Equals(object obj)
    {
        if (obj is StringTelemetry other)
        {
            bool timestampsEqual = (Timestamp - other.Timestamp).TotalMilliseconds < 1;

            bool namesEqual = (Name?.TrimEnd('\0') == other.Name?.TrimEnd('\0'));

            return timestampsEqual && namesEqual;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Timestamp, Name);
    }
}
