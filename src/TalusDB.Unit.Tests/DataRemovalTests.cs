using TalusDB.Unit.Tests.TestEntities;
using WildernessLabs.TalusDB;

namespace TalusDB.Unit.Tests
{
    public class DataRemovalTests : TestBase
    {
        [Fact]
        public void FillAndEmpty()
        {
            var db = new Database();

            // clean house - unknown start state so don't test results
            DropAllTables(db);

            var t = db.CreateTable<IndexValueTelemetry>(10);

            var lowWaterFired = false;
            var underrunFired = false;

            t.LowWaterLevel = 2;
            t.LowWater += (s, e) =>
            {
                lowWaterFired = true;
            };
            t.Underrun += (s, e) =>
            {
                underrunFired = true;
            };

            t.Truncate();

            // add 5
            for (var i = 0; i < 5; i++)
            {
                Assert.Equal(i, t.Count);

                var item = new IndexValueTelemetry
                {
                    Timestamp = DateTime.Now,
                    Index = i,
                    Value = i * 1.1f
                };

                t.Insert(item);

                Assert.False(lowWaterFired);
            }

            // remove 2
            var item0 = t.Remove();
            Assert.False(lowWaterFired);
            var item1 = t.Remove();
            Assert.False(lowWaterFired);

            // event will fire when we reach low water
            var item2 = t.Remove();
            Assert.True(lowWaterFired);
            Assert.False(underrunFired);
            Assert.False(t.HasUnderrun);

            var item3 = t.Remove();
            var item4 = t.Remove();

            Assert.False(underrunFired);
            Assert.False(t.HasUnderrun);

            // the table is now empty
            Assert.Equal(0, t.Count);
            // Select should return nothing
            var item5 = t.Remove();
            Assert.Null(item5);
            // and fire the underrun
            Assert.True(underrunFired);
            Assert.True(t.HasUnderrun);
        }
    }
}