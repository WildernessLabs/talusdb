namespace TalusDB.Unit.Tests.TestEntities
{
    public struct IndexValueTelemetry : IEquatable<IndexValueTelemetry>
    {
        public DateTime Timestamp { get; set; }
        public int Index { get; set; }
        public float Value { get; set; }

        public bool Equals(IndexValueTelemetry other)
        {
            if (Timestamp != other.Timestamp) return false;
            if (Index != other.Index) return false;
            if (Value != other.Value) return false;

            return true;
        }
    }
}