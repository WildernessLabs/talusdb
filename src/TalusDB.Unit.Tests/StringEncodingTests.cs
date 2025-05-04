using System.Runtime.InteropServices;
using WildernessLabs.TalusDB;

namespace TalusDB.Unit.Tests;

public struct EncodingTestTelemetry
{
    public DateTime Timestamp;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
    public string Text;

    public override bool Equals(object obj)
    {
        if (obj is EncodingTestTelemetry other)
        {
            bool timestampsEqual = (Timestamp - other.Timestamp).TotalMilliseconds < 1;

            return timestampsEqual &&
                   Text?.TrimEnd('\0') == other.Text?.TrimEnd('\0');
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Timestamp, Text);
    }
}

public class StringEncodingTests : TestBase
{
    [Fact]
    public void BasicEncodingTest()
    {
        var db = DatabaseFactory.GetDatabase();
        DropAllTables(db);

        var t = db.CreateTable<EncodingTestTelemetry>(10);

        // Basic ASCII test
        var asciiTest = new EncodingTestTelemetry
        {
            Timestamp = DateTime.Now,
            Text = "This is a basic ASCII string."
        };

        t.Insert(asciiTest);
        var result = t.Remove();

        Assert.NotNull(result);
        Assert.Equal(asciiTest.Text, result.Value.Text.TrimEnd('\0'));
    }

    [Fact]
    public void ExtendedLatinCharactersTest()
    {
        var db = DatabaseFactory.GetDatabase();
        DropAllTables(db);

        var t = db.CreateTable<EncodingTestTelemetry>(10);

        // Test with extended Latin characters
        var extendedLatinText = "Café où naïve œuvre résumé décès éléphant";
        var item = new EncodingTestTelemetry
        {
            Timestamp = DateTime.Now,
            Text = extendedLatinText
        };

        t.Insert(item);
        var result = t.Remove();

        Assert.NotNull(result);
        Assert.Equal(extendedLatinText, result.Value.Text.TrimEnd('\0'));
    }

    [Fact]
    public void LongStringTest()
    {
        var db = DatabaseFactory.GetDatabase();
        DropAllTables(db);

        var t = db.CreateTable<EncodingTestTelemetry>(10);

        // Test with a very long string (at capacity)
        var longText = new string('X', 99); // 99 chars + null terminator = 100
        var item = new EncodingTestTelemetry
        {
            Timestamp = DateTime.Now,
            Text = longText
        };

        t.Insert(item);
        var result = t.Remove();

        Assert.NotNull(result);
        Assert.Equal(longText, result.Value.Text.TrimEnd('\0'));
    }
}