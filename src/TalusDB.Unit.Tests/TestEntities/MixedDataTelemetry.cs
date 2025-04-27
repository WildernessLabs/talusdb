using System.Runtime.InteropServices;

namespace TalusDB.Unit.Tests.TestEntities;

// Struct with mixed data types including a string
public struct MixedDataTelemetry
{
    public DateTime Timestamp;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
    public string Name;
    public float Value;
    public bool IsEnabled;
    public int Count;

    public override bool Equals(object obj)
    {
        if (obj is MixedDataTelemetry other)
        {
            bool timestampsEqual = (Timestamp - other.Timestamp).TotalMilliseconds < 1;

            return timestampsEqual &&
                   Name?.TrimEnd('\0') == other.Name?.TrimEnd('\0') &&
                   Value == other.Value &&
                   IsEnabled == other.IsEnabled &&
                   Count == other.Count;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Timestamp, Name, Value, IsEnabled, Count);
    }
}
