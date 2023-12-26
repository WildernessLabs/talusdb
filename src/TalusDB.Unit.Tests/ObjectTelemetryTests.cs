using TalusDB.Unit.Tests.TestEntities;
using WildernessLabs.TalusDB;

namespace TalusDB.Unit.Tests
{
    public class ObjectTelemetryTests : TestBase
    {
        [Fact]
        public void AddOne()
        {
            var db = new Database();

            // clean house - unknown start state so don't test results
            DropAllTables(db);

            var t = db.CreateTable2<ObjectTelemetry>(100);

            //t.Truncate();

            Assert.Equal(0, t.Count);

            var source = new ObjectTelemetry
            {
                Timestamp = DateTime.Now,
                Name = "This is a name",
                Description = "This is a description. "
                + "1234567890123456789012345678901234567890",
                Value = 42.42
            };
            t.Insert(source);

            Assert.Equal(1, t.Count);

            /*
            t.Truncate();

            Assert.Equal(0, t.Count);

            var source = new TempTelemetry
            {
                Timestamp = DateTime.Now,
                Temp = 43.21
            };

            t.Insert(source);
            Assert.Equal(1, t.Count);
            */
        }

        [Fact]
        public void AddAndRetrieveOne()
        {
            var db = new Database();

            // clean house - unknown start state so don't test results
            DropAllTables(db);

            var t = db.CreateTable2<ObjectTelemetry>(100);

            //t.Truncate();

            Assert.Equal(0, t.Count);

            var source = new ObjectTelemetry
            {
                Timestamp = DateTime.Now,
                Name = "This is a name",
                Description = "This is a description.",
                Value = 42.42
            };
            t.Insert(source);

            Assert.Equal(1, t.Count);

            var value = t.Remove();

            Assert.Equal(0, t.Count);
            /*
            t.Truncate();

            Assert.Equal(0, t.Count);

            var source = new TempTelemetry
            {
                Timestamp = DateTime.Now,
                Temp = 43.21
            };

            t.Insert(source);
            Assert.Equal(1, t.Count);
            */
        }

        [Fact]
        public void AddAndRetrieveTwo()
        {
            var db = new Database();

            // clean house - unknown start state so don't test results
            DropAllTables(db);

            var t = db.CreateTable2<ObjectTelemetry>(100);

            //t.Truncate();

            Assert.Equal(0, t.Count);

            var o1 = new ObjectTelemetry
            {
                Timestamp = DateTime.Now,
                Name = "Name 1",
                Description = "This is a description.",
                Value = 42.42
            };
            t.Insert(o1);
            var o2 = new ObjectTelemetry
            {
                Timestamp = DateTime.Now,
                Name = "Name 2",
                Description = "This is another description.",
                Value = 24.24
            };
            t.Insert(o2);

            Assert.Equal(2, t.Count);
            var test1 = t.Remove();
            Assert.Equal(1, t.Count);
            Assert.Equal(o1, test1);

            var test2 = t.Remove();
            Assert.Equal(0, t.Count);
            Assert.Equal(o2, test2);
        }

        [Fact]
        public void AddAndRetrieveWithWrap()
        {
            var db = new Database();

            // clean house - unknown start state so don't test results
            DropAllTables(db);

            var t = db.CreateTable2<ObjectTelemetry>(30);

            Assert.Equal(0, t.Count);

            // insert 2 items
            var o1 = new ObjectTelemetry
            {
                Timestamp = DateTime.Now,
                Name = "Name 1",
                Description = "This is a long description. 123456789012345678901234567890123456789012345678901234567890",
                Value = 42.42
            };
            t.Insert(o1);
            var o2 = new ObjectTelemetry
            {
                Timestamp = DateTime.Now,
                Name = "Name 2",
                Description = "This is another long description. 123456789012345678901234567890123456789012345678901234567890",
                Value = 24.24
            };
            t.Insert(o2);

            // remove the tail item
            Assert.Equal(2, t.Count);
            var test1 = t.Remove();
            Assert.Equal(1, t.Count);

            // now add a head item, which should wrap and write into where the tail was

            var o3 = new ObjectTelemetry
            {
                Timestamp = DateTime.Now,
                Name = "Name 3",
                Description = "This is another long description. 123456789012345678901234567890123456789012345678901234567890",
                Value = 12.34
            };
            t.Insert(o3);
            Assert.Equal(2, t.Count);

            var test2 = t.Remove();
            Assert.Equal(1, t.Count);
            Assert.Equal(o2, test2);

            var test3 = t.Remove();
            Assert.Equal(0, t.Count);
            Assert.Equal(o3, test3);
        }
    }
}