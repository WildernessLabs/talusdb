using TalusDB.Unit.Tests.TestEntities;
using WildernessLabs.TalusDB;

namespace TalusDB.Unit.Tests;

public class StringElementTests : TestBase
{
    [Fact]
    public void NonBlittablePropertyType()
    {
        var db = new Database();

        // clean house - unknown start state so don't test results
        DropAllTables(db);

        Assert.Throws<TalusException>(() =>
        {
            var t = db.CreateTable<InvalidPropertyTypeTelemetry>(10);
        });
    }

    [Fact]
    public void InvalidStringProperty()
    {
        var db = new Database();

        // clean house - unknown start state so don't test results
        DropAllTables(db);

        Assert.Throws<TalusException>(() =>
        {
            var t = db.CreateTable<InvalidStringPropertyTelemetry>(10);
        });
    }

    [Fact]
    public void InvalidStringField()
    {
        var db = new Database();

        // clean house - unknown start state so don't test results
        DropAllTables(db);

        Assert.Throws<TalusException>(() =>
        {
            var t = db.CreateTable<InvalidStringFieldTelemetry>(10);
        });
    }

    [Fact]
    public void AddStringItem()
    {
        var db = new Database();

        // clean house - unknown start state so don't test results
        DropAllTables(db);

        var t = db.CreateTable<StringTelemetry>(10);

        Assert.NotNull(t);
        Assert.Equal(0, t.Count);

        var item = new StringTelemetry
        {
            Timestamp = DateTime.Now.ToUniversalTime(),
            Name = "Hello World",
        };

        t.Insert(item);

        Assert.Equal(1, t.Count);

        var item2 = t.Remove();
        Assert.Equal(0, t.Count);
        Assert.NotNull(item2);

        Assert.Equal(item, item2.Value);

    }

    [Fact]
    public void MixedItemTest()
    {
        var db = new Database();

        // clean house - unknown start state so don't test results
        DropAllTables(db);

        var t = db.CreateTable<MixedPropertyAndFieldType>(10);

        Assert.NotNull(t);
        Assert.Equal(0, t.Count);

        var item = new MixedPropertyAndFieldType
        {
            Timestamp = DateTime.Now,
            Name = "Hello World",
        };

        t.Insert(item);

        Assert.Equal(1, t.Count);

        var item2 = t.Remove();
        Assert.Equal(0, t.Count);
        Assert.NotNull(item2);

        Assert.Equal(item, item2.Value);

    }
}