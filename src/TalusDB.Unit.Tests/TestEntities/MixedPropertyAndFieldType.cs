using System.Runtime.InteropServices;

namespace TalusDB.Unit.Tests.TestEntities;

// Struct with mixed property and field types
public struct MixedPropertyAndFieldType
{
    public DateTime Timestamp { get; set; }
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
    public string Name;

    public override bool Equals(object obj)
    {
        if (obj is MixedPropertyAndFieldType other)
        {
            bool timestampsEqual = (Timestamp - other.Timestamp).TotalMilliseconds < 1;

            return timestampsEqual &&
                   Name?.TrimEnd('\0') == other.Name?.TrimEnd('\0');
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Timestamp, Name);
    }
}
