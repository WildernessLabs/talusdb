using System.Diagnostics;
using TalusDB.Unit.Tests.TestEntities;
using WildernessLabs.TalusDB;

namespace TalusDB.Unit.Tests;

public class TestPublisher : PublisherBase

{
    public int PublishedRecordCount { get; private set; } = 0;

    public event EventHandler ItemPublished = delegate { };

    public TestPublisher(Database database)
        : base(database)
    {
        PublicationPeriod = TimeSpan.FromSeconds(5);
    }

    public override async Task<bool> PublishItem(object item)
    {
        await Task.Delay(100);
        Debug.WriteLine($"{item.GetType().Name} published");
        PublishedRecordCount++;
        ItemPublished?.Invoke(this, EventArgs.Empty);

        return true;
    }
}

public class PublisherTests : TestBase
{
    [Fact]
    public void CreateAndDropTableTests()
    {
        var db = new Database();

        var pubCount = 0;

        var pub = new TestPublisher(db);
        pub.ItemPublished += (sender, e) => pubCount++;

        // clean house - unknown start state so don't test results
        DropAllTables(db);

        var t = db.CreateTable<TempTelemetry>(10);

        var itemsToAdd = 8;

        for (var i = 0; i < itemsToAdd; i++)
        {
            t.Insert(new TempTelemetry
            {
                Timestamp = DateTime.Now.AddSeconds(i),
                Temp = i * 7f
            }); ;
        }

        Assert.Equal(itemsToAdd, t.Count);

        t.PublicationEnabled = true;
        Thread.Sleep(pub.PublicationPeriod.Add(TimeSpan.FromSeconds(1)));

        Assert.Equal(0, t.Count);
        Assert.Equal(itemsToAdd, pubCount);

    }
}