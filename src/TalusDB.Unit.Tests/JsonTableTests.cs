using WildernessLabs.TalusDB;

namespace TalusDB.Unit.Tests;

public class JsonTableTests : TestBase
{
    [Fact]
    public void BasicJsonTableFunctionality()
    {
        // Create a database
        var db = new Database();

        // Clean up any existing tables
        DropAllTables(db);

        // Create a JsonTable for storing SensorData objects
        var jsonTable = db.CreateTable<SensorData>(100);

        // Verify the table was created
        Assert.NotNull(jsonTable);
        Assert.Equal(0, jsonTable.Count);

        // Create a test object
        var testData = new SensorData
        {
            Timestamp = DateTime.Now,
            DeviceId = "device-123",
            Temperature = 23.5,
            Humidity = 45.8,
            Tags = new List<string> { "indoor", "test" }
        };

        // Insert the object into the table
        jsonTable.Insert(testData);

        // Verify the insertion
        Assert.Equal(1, jsonTable.Count);

        // Retrieve the object
        var retrievedData = jsonTable.Remove();

        // Verify the retrieval
        Assert.NotNull(retrievedData);
        Assert.Equal(0, jsonTable.Count);

        // Verify the data matches
        Assert.Equal(testData.DeviceId, retrievedData.DeviceId);
        Assert.Equal(testData.Temperature, retrievedData.Temperature);
        Assert.Equal(testData.Humidity, retrievedData.Humidity);

        // Verify complex property (List<string>)
        Assert.NotNull(retrievedData.Tags);
        Assert.Equal(testData.Tags.Count, retrievedData.Tags.Count);
        for (int i = 0; i < testData.Tags.Count; i++)
        {
            Assert.Equal(testData.Tags[i], retrievedData.Tags[i]);
        }
    }
}

// Sample class for testing JsonTable
public class SensorData
{
    public DateTime Timestamp { get; set; }
    public string DeviceId { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public List<string> Tags { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is SensorData other)
        {
            bool tagsEqual = true;
            if (Tags != null && other.Tags != null)
            {
                if (Tags.Count == other.Tags.Count)
                {
                    for (int i = 0; i < Tags.Count; i++)
                    {
                        if (Tags[i] != other.Tags[i])
                        {
                            tagsEqual = false;
                            break;
                        }
                    }
                }
                else
                {
                    tagsEqual = false;
                }
            }
            else
            {
                tagsEqual = (Tags == null && other.Tags == null);
            }

            return Timestamp.Ticks == other.Timestamp.Ticks &&
                   DeviceId == other.DeviceId &&
                   Math.Abs(Temperature - other.Temperature) < 0.001 &&
                   Math.Abs(Humidity - other.Humidity) < 0.001 &&
                   tagsEqual;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Timestamp, DeviceId, Temperature, Humidity, Tags);
    }
}