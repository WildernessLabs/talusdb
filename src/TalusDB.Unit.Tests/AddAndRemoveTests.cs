using TalusDB.Unit.Tests.TestEntities;
using WildernessLabs.TalusDB;

namespace TalusDB.Unit.Tests
{
    public class AddAndRemoveTests : TestBase
    {
        [Fact]
        public void AddAndRemoveOne()
        {
            var db = new Database();

            // clean house - unknown start state so don't test results
            DropAllTables(db);

            var t = db.CreateTable<TempTelemetry>(10);

            t.Truncate();

            Assert.Equal(0, t.Count);

            var source = new TempTelemetry
            {
                Timestamp = DateTime.Now,
                Temp = 43.21
            };

            t.Insert(source);
            Assert.Equal(1, t.Count);
            var test = t.Select();
            Assert.Equal(source, test);
            Assert.Equal(0, t.Count);
        }

        [Fact]
        public void AddAndRemoveTwo()
        {
            var db = new Database();

            // clean house - unknown start state so don't test results
            DropAllTables(db);

            var t = db.CreateTable<TempTelemetry>(10);

            t.Truncate();

            Assert.Equal(0, t.Count);

            var source1 = new TempTelemetry
            {
                Timestamp = DateTime.Now,
                Temp = 43.21
            };
            var source2 = new TempTelemetry
            {
                Timestamp = DateTime.Now.AddMinutes(1),
                Temp = 12.34
            };

            t.Insert(source1);
            Assert.Equal(1, t.Count);
            t.Insert(source2);
            Assert.Equal(2, t.Count);

            var test = t.Select();
            Assert.Equal(source1, test);
            Assert.Equal(1, t.Count);
            test = t.Select();
            Assert.Equal(source2, test);
            Assert.Equal(0, t.Count);
        }
    }
}