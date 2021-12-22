using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neo4jClient;
using Shared;
using Tweetinvi;
using Tweetinvi.Streaming.V2;
using Tweetinvi.Streams;
using TwitterTest;
using TwitterTest.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
        config.AddEnvironmentVariables())
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();

        //add DBs
        services.AddSingleton<IGraphClient>(ConnectionCreator.Neo4J());
        services.AddSingleton(ConnectionCreator.Mongo());
        //add APIs
        services.AddSingleton(static (services) =>
            ConnectionCreator.TwitterApi(services.GetRequiredService<IConfiguration>()));

        services.AddSingleton<ITweetFilter>(
            new TweetFilter()
                .AllowLanguage("de", "en", "ja")
                // .IgnoreTweetTypes(TweetType.Retweet)
                .IgnoreIf(tweet => (tweet.Entities.Hashtags is null || !tweet.Entities.Hashtags.Any()) &&
                                   (tweet.ContextAnnotations is null || !tweet.ContextAnnotations.Any()))
        );

        services.AddSingleton<Neo4JInserter>();


        //add ISampleStreamV2 implementation based on config, either use the TwitterAPI, or use MongoDB
        services.AddSingleton(static (services) =>
            (SampleStreamV2)services.GetRequiredService<TwitterClient>().StreamsV2.CreateSampleStream());
        services.AddSingleton<MongoSampleStream>();
        services.AddSingleton<ServiceResolver<ISampleStreamV2>>(serviceProvider => () =>
        {
            ISampleStreamV2 implementation = serviceProvider.GetRequiredService<IConfiguration>()["StreamProvider"] switch
            {
                "TwitterApi" => serviceProvider.GetRequiredService<SampleStreamV2>(),
                "MongoDb" => serviceProvider.GetRequiredService<MongoSampleStream>(),
                _ => throw new InvalidOperationException()
            };

            serviceProvider.GetRequiredService<ILogger<Program>>()
                .LogWarning("using {} as {}", implementation.GetType(), nameof(ISampleStreamV2));
            return implementation;
        });


        //add IMongoInserter implementation based on config. We use the NullInserter if we use MongoDB as our data source,
        //as we don't want to create a feedback loop
        services.AddSingleton<MongoNullInserter>();
        services.AddSingleton<MongoInserter>();
        services.AddSingleton<ServiceResolver<IMongoInserter>>(serviceProvider => () =>
        {
            IMongoInserter implementation = serviceProvider.GetRequiredService<IConfiguration>()["StreamProvider"] switch
            {
                "TwitterApi" => serviceProvider.GetRequiredService<MongoInserter>(),
                "MongoDb" => serviceProvider.GetRequiredService<MongoNullInserter>(),
                _ => throw new InvalidOperationException()
            };

            serviceProvider.GetRequiredService<ILogger<Program>>()
                .LogWarning("using {} as {}", implementation.GetType(), nameof(IMongoInserter));
            return implementation;
        });
    })
    .Build();

await host.RunAsync();


public delegate T ServiceResolver<T>();
