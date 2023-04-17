namespace TalusDB.Unit.Tests.TestEntities
{
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