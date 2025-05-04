using TalusDB.Unit.Tests.TestEntities;
using WildernessLabs.TalusDB;

namespace TalusDB.Unit.Tests;

public static class DatabaseFactory
{
    public static Database GetDatabase()
    {
        // gnerate using a random name to prevent test collisions on the same files
        string currentDirectory = Environment.CurrentDirectory;
        var testDirectory = Path.Combine(currentDirectory, Path.GetRandomFileName());
        Directory.CreateDirectory(testDirectory);
        return new Database(testDirectory);
    }
}

public class DatabaseTests : TestBase
{
    [Fact]
    public void CreateAndDropTableTests()
    {
        var db = DatabaseFactory.GetDatabase();

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