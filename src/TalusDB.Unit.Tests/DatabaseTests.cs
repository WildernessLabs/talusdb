using TalusDB.Unit.Tests.TestEntities;
using WildernessLabs.TalusDB;

namespace TalusDB.Unit.Tests
{
    public class DatabaseTests : TestBase
    {
        [Fact]
        public void CreateAndDropTableTests()
        {
            var db = new Database();

            // clean house - unknown start state so don't test results
            DropAllTables(db);

            var t = db.CreateTable<IndexValueTelemetry>(10);
            Assert.NotNull(t);
            Assert.Equal(10, t.MaxElements);
            Assert.Equal(0, t.Count);
            var t2 = db.GetTable<IndexValueTelemetry>();
            Assert.Equal(t, t2);

            var names = db.GetTableNames();
            Assert.NotEmpty(names);
            // drop by type
            db.DropTable<IndexValueTelemetry>();
            names = db.GetTableNames();
            Assert.Empty(names);

            db.CreateTable<IndexValueTelemetry>(10);
            Assert.NotNull(t);
            names = db.GetTableNames();
            Assert.NotEmpty(names);
            // drop by name
            db.DropTable(nameof(IndexValueTelemetry));
            names = db.GetTableNames();
            Assert.Empty(names);
        }
    }
}