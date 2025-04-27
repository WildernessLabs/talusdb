using System.Runtime.InteropServices;

namespace TalusDB.Unit.Tests.TestEntities;

// Struct with multiple string fields
public struct MultiStringTelemetry
{
    public DateTime Timestamp;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
    public string Name;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
    public string Description;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
    public string Location;

    public override bool Equals(object obj)
    {
        if (obj is MultiStringTelemetry other)
        {
            return Timestamp.ToUniversalTime() == other.Timestamp.ToUniversalTime() &&
                   Name?.TrimEnd('\0') == other.Name?.TrimEnd('\0') &&
                   Description?.TrimEnd('\0') == other.Description?.TrimEnd('\0') &&
                   Location?.TrimEnd('\0') == other.Location?.TrimEnd('\0');
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Timestamp, Name, Description, Location);
    }
}
