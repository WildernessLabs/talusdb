namespace TalusDB.Unit.Tests.TestEntities;

public struct InvalidStringFieldTelemetry
{
    public DateTime Timestamp;
    // note: no MarshalAs, so it's not a fixed-size
    public string Name;
}