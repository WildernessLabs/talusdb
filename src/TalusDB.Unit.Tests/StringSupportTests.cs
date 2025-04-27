using TalusDB.Unit.Tests.TestEntities;
using WildernessLabs.TalusDB;

namespace TalusDB.Unit.Tests;

public class StringSupportTests : TestBase
{
    [Fact]
    public void StringLengthTest()
    {
        var db = new Database();
        DropAllTables(db);

        var t = db.CreateTable<StringTelemetry>(10);

        // Test with string at exact capacity
        var maxLengthString = new string('A', 49); // Assuming StringTelemetry.Name has SizeConst=50, we need space for a null terminator
        var item1 = new StringTelemetry
        {
            Timestamp = DateTime.Now,
            Name = maxLengthString
        };

        t.Insert(item1);
        var result1 = t.Remove();

        Assert.NotNull(result1);
        Assert.Equal(maxLengthString, result1.Value.Name);
    }

    [Fact]
    public void StringTruncationTest()
    {
        var db = new Database();
        DropAllTables(db);

        var t = db.CreateTable<StringTelemetry>(10);

        // Test with string longer than capacity (should be truncated)
        var tooLongString = new string('B', 100); // Longer than the expected 50 char limit
        var item = new StringTelemetry
        {
            Timestamp = DateTime.Now,
            Name = tooLongString
        };

        t.Insert(item);
        var result = t.Remove();

        Assert.NotNull(result);
        Assert.NotEqual(tooLongString, result.Value.Name);
        Assert.Equal(tooLongString.Substring(0, 49), result.Value.Name.TrimEnd('\0')); // Account for null terminator
    }

    [Fact]
    public void EmptyStringTest()
    {
        var db = new Database();
        DropAllTables(db);

        var t = db.CreateTable<StringTelemetry>(10);

        // Test with empty string
        var item = new StringTelemetry
        {
            Timestamp = DateTime.Now,
            Name = string.Empty
        };

        t.Insert(item);
        var result = t.Remove();

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Value.Name.TrimEnd('\0'));
    }

    [Fact]
    public void NullStringTest()
    {
        var db = new Database();
        DropAllTables(db);

        var t = db.CreateTable<StringTelemetry>(10);

        // Test with null string
        var item = new StringTelemetry
        {
            Timestamp = DateTime.Now,
            Name = null
        };

        t.Insert(item);
        var result = t.Remove();

        Assert.NotNull(result);
        // Null string should be stored as empty
        Assert.Equal(string.Empty, result.Value.Name.TrimEnd('\0'));
    }

    [Fact]
    public void MultipleStringFieldsTest()
    {
        var db = new Database();
        DropAllTables(db);

        var t = db.CreateTable<MultiStringTelemetry>(10);

        var item = new MultiStringTelemetry
        {
            Timestamp = DateTime.Now,
            Name = "Device Name",
            Description = "This is a device description",
            Location = "Building A, Floor 3, Room 405"
        };

        t.Insert(item);
        var result = t.Remove();

        Assert.NotNull(result);
        Assert.Equal(item.Name, result.Value.Name.TrimEnd('\0'));
        Assert.Equal(item.Description, result.Value.Description.TrimEnd('\0'));
        Assert.Equal(item.Location, result.Value.Location.TrimEnd('\0'));
    }

    [Fact]
    public void SpecialCharactersTest()
    {
        var db = new Database();
        DropAllTables(db);

        var t = db.CreateTable<StringTelemetry>(10);

        // Test with special characters
        var specialString = "!@#$%^&*()_+-={}[]|\\:;\"'<>,.?/~`";
        var item = new StringTelemetry
        {
            Timestamp = DateTime.Now,
            Name = specialString
        };

        t.Insert(item);
        var result = t.Remove();

        Assert.NotNull(result);
        Assert.Equal(specialString, result.Value.Name.TrimEnd('\0'));
    }

    [Fact]
    public void MaxCapacityTest()
    {
        var db = new Database();
        DropAllTables(db);

        // Create table with small max elements
        var t = db.CreateTable<StringTelemetry>(3);

        // Fill table to capacity
        for (int i = 0; i < 3; i++)
        {
            var item = new StringTelemetry
            {
                Timestamp = DateTime.Now.AddMinutes(i),
                Name = $"String data {i}"
            };
            t.Insert(item);
        }

        Assert.Equal(3, t.Count);

        // Add one more record (should cause oldest to be overwritten)
        var overflowItem = new StringTelemetry
        {
            Timestamp = DateTime.Now.AddMinutes(10),
            Name = "Overflow data"
        };
        t.Insert(overflowItem);

        Assert.Equal(3, t.Count);

        // First record should be gone, second should be first now
        var result = t.Remove();
        Assert.NotNull(result);
        Assert.Equal("String data 1", result.Value.Name.TrimEnd('\0'));
    }

    [Fact]
    public void MixedDataTypesTest()
    {
        var db = new Database();
        DropAllTables(db);

        var t = db.CreateTable<MixedDataTelemetry>(10);

        var now = DateTime.Now;
        var item = new MixedDataTelemetry
        {
            Timestamp = now,
            Name = "Sensor Alpha",
            Value = 42.5f,
            IsEnabled = true,
            Count = 123
        };

        t.Insert(item);
        var result = t.Remove();

        Assert.NotNull(result);
        Assert.Equal(item.Name, result.Value.Name.TrimEnd('\0'));
        Assert.Equal(item.Value, result.Value.Value);
        Assert.Equal(item.IsEnabled, result.Value.IsEnabled);
        Assert.Equal(item.Count, result.Value.Count);
    }
}