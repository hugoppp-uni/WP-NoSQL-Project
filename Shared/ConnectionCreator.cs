using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4jClient;
using Tweetinvi;
using Tweetinvi.Models;

namespace Shared;

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

    public static TwitterClient TwitterApi(IConfiguration config)
    {
        return new TwitterClient(CreateCredentials(config));

        static IReadOnlyConsumerCredentials CreateCredentials(IConfiguration config)
        {
            string bearerToken = config["TWITTER_BEARER_TOKEN"] ??
                                 throw new InvalidOperationException($"Configuration is missing bearerToken");
            string consumerSecret = config["TWITTER_CONSUMER_SECRET"] ??
                                    throw new InvalidOperationException($"Configuration is missing consumerSecret");
            string consumerKey = config["TWITTER_CONSUMER_KEY"] ??
                                 throw new InvalidOperationException($"Configuration is missing consumerKey");

            return new ReadOnlyConsumerCredentials(consumerKey, consumerSecret, bearerToken);
        }
    }
}
