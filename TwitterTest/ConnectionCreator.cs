using MongoDB.Bson;
using MongoDB.Driver;
using Neo4jClient;

namespace TwitterTest;

public static class ConnectionCreator
{

    private static bool IsLocalDevEnvironment => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == null;

    public static IMongoClient Mongo(int secondsTimeout = 2)
    {
        string connectionString = IsLocalDevEnvironment ? "mongodb://localhost:27017" : "mongodb://mongo-db:27017";
        var client = new MongoClient(connectionString);
        bool connected = client.GetDefaultDatabase().RunCommandAsync((Command<BsonDocument>)"{ping:1}")
            .Wait(TimeSpan.FromSeconds(secondsTimeout));

        if (!connected)
            throw new InvalidOperationException($"Could not connect to Mongo (timeout after {secondsTimeout} seconds)");

        return client;
    }

    public static GraphClient Neo4J(int secondsTimeout = 2)
    {
        try
        {
            GraphClient client = new GraphClient(new Uri("http://localhost:7474"), "neo4j", "twitter");
            bool connected = client.ConnectAsync().Wait(TimeSpan.FromSeconds(secondsTimeout));
            if (!connected)
                throw new InvalidOperationException($"Could not connect to neo4j (timeout after {secondsTimeout} seconds)");

            return client;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Could not connect to neo4j", e);
        }
    }

}
