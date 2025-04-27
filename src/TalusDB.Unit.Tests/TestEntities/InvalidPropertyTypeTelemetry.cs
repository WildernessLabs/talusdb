namespace TalusDB.Unit.Tests.TestEntities;

// Struct with a string property (which is not valid)
public struct InvalidPropertyTypeTelemetry
{
    public DateTime Timestamp { get; set; }
    public string Name { get; set; }
}