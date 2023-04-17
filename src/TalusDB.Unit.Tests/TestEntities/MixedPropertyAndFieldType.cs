using System.Runtime.InteropServices;

namespace TalusDB.Unit.Tests.TestEntities
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MixedPropertyAndFieldType
    {
        public DateTime Timestamp { get; set; }

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Name;

        public bool Equals(MixedPropertyAndFieldType other)
        {
            if (Timestamp.Ticks != other.Timestamp.Ticks) return false;
            if (Name != other.Name) return false;
            return true;
        }
    }
}