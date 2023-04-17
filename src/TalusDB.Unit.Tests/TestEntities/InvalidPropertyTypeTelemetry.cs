namespace TalusDB.Unit.Tests.TestEntities
{
    public struct InvalidPropertyTypeTelemetry
    {
        public class Foo
        {
            public string Name { get; set; }
        }

        public DateTime Timestamp { get; set; }
        public Foo bar { get; set; }
    }
}