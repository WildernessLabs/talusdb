using System.Text;

namespace TalusDB.Unit.Tests.TestEntities
{
    public struct StringTelemetry : IEquatable<StringTelemetry>
    {
        public DateTime Timestamp { get; set; }
        public byte[] NameData { get; set; }

        public StringTelemetry()
        {
            Timestamp = default;
            NameData = new byte[16];
        }

        public string Name
        {
            get => Encoding.UTF8.GetString(NameData, 0, NameData.Length).Trim();
            set
            {
                Array.Clear(NameData);
                var data = Encoding.UTF8.GetBytes(value);
                Array.Copy(data, 0, NameData, 0, data.Length < 16 ? data.Length : 16);
            }
        }

        public bool Equals(StringTelemetry other)
        {
            if (Timestamp != other.Timestamp) return false;
            if (Name != other.Name) return false;
            return true;
        }
    }

    public struct TempTelemetry : IEquatable<TempTelemetry>
    {
        public DateTime Timestamp { get; set; }
        public double Temp { get; set; }

        public bool Equals(TempTelemetry other)
        {
            if (Timestamp != other.Timestamp) return false;
            if (Temp != other.Temp) return false;
            return true;
        }
    }
}