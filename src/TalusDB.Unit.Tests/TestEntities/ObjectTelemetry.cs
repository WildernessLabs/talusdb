namespace TalusDB.Unit.Tests.TestEntities
{
    public class ObjectTelemetry : IEquatable<ObjectTelemetry>
    {
        public DateTime Timestamp { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Value { get; set; }

        public bool Equals(ObjectTelemetry other)
        {
            if (other == null) return false;

            if (Timestamp.Ticks != other?.Timestamp.Ticks) return false;
            if (Name != other?.Name) return false;
            if (Description != other?.Description) return false;
            if (Value != other?.Value) return false;
            return true;
        }
    }
}