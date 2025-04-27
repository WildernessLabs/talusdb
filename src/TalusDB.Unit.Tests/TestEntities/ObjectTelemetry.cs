namespace TalusDB.Unit.Tests.TestEntities;

public class ObjectTelemetry
{
    public DateTime Timestamp { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public double Value { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is ObjectTelemetry other)
        {
            return Name == other.Name &&
                   Description == other.Description &&
                   Value == other.Value;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Timestamp, Name, Description, Value);
    }
}
