using TalusDB.Unit.Tests.TestEntities;
using WildernessLabs.TalusDB;

namespace TalusDB.Unit.Tests
{
    public class DataInsertTests : TestBase
    {
        [Fact]
        public void AddOne()
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
        }

        [Fact]
        public void FillTableTest()
        {
            var db = new Database();

            // clean house - unknown start state so don't test results
            DropAllTables(db);

            var t = db.CreateTable<IndexValueTelemetry>(5);

            t.Truncate();

            for (var i = 0; i < t.MaxElements; i++)
            {
                Assert.Equal(i, t.Count);

                var item = new IndexValueTelemetry
                {
                    Timestamp = DateTime.Now,
                    Index = i,
                    Value = i * 1.1f
                };

                t.Insert(item);
            }

            Assert.Equal(t.MaxElements, t.Count);
        }

        [Fact]
        public void OverrunTest()
        {
            var db = new Database();

            // clean house - unknown start state so don't test results
            DropAllTables(db);

            var t = db.CreateTable<IndexValueTelemetry>(5);
            var overrunFired = false;

            t.Overrun += (s, e) =>
            {
                overrunFired = true;
            };

            t.Truncate();

            IndexValueTelemetry item;

            for (var i = 0; i < t.MaxElements; i++)
            {
                Assert.Equal(i, t.Count);

                item = new IndexValueTelemetry
                {
                    Timestamp = DateTime.Now,
                    Index = i,
                    Value = i * 1.1f
                };

                t.Insert(item);

                Assert.False(overrunFired);
            }

            Assert.Equal(t.MaxElements, t.Count);

            item = new IndexValueTelemetry
            {
                Timestamp = DateTime.Now,
                Index = t.MaxElements,
                Value = t.MaxElements * 1.1f
            };

            // append one more than max
            t.Insert(item);

            Assert.True(overrunFired);

            // should still be at max
            Assert.Equal(t.MaxElements, t.Count);

            // oldest should be index 1 (index 0 was overwritten)
            var test = t.Select();
            Assert.NotNull(test);
            Assert.Equal(1, test.Value.Index);
        }

        [Fact]
        public void HighWaterTest()
        {
            var db = new Database();

            // clean house - unknown start state so don't test results
            DropAllTables(db);

            var t = db.CreateTable<IndexValueTelemetry>(10);
            t.HighWaterLevel = 8;

            var highWaterFired = false;

            t.HighWater += (s, e) =>
            {
                highWaterFired = true;
            };

            t.Truncate();

            IndexValueTelemetry item;

            for (var i = 0; i < t.HighWaterLevel - 1; i++)
            {
                Assert.Equal(i, t.Count);

                item = new IndexValueTelemetry
                {
                    Timestamp = DateTime.Now,
                    Index = i,
                    Value = i * 1.1f
                };

                t.Insert(item);

                Assert.False(highWaterFired);
            }

            item = new IndexValueTelemetry
            {
                Timestamp = DateTime.Now,
                Index = t.MaxElements,
                Value = t.MaxElements * 1.1f
            };

            // append one more than high water
            t.Insert(item);

            Assert.True(highWaterFired);
        }
    }
}