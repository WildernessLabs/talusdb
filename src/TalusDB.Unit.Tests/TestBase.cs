using WildernessLabs.TalusDB;

namespace TalusDB.Unit.Tests;

public abstract class TestBase
{
    protected void DropAllTables(Database db)
    {
        var names = db.GetTableNames();
        foreach (var name in names)
        {
            db.DropTable(name);
        }
    }
}