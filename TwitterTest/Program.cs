using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Neo4jClient;
using Tweetinvi;
using Tweetinvi.Core.Streaming.V2;
using Tweetinvi.Models.V2;
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

        services.AddSingleton<IGraphClient>(ConnectionCreator.Neo4J());


        var mongoClient = ConnectionCreator.Mongo();
        services.AddSingleton(mongoClient);

        services.AddSingleton(static (services) =>
            ConnectionCreator.TwitterApi(services.GetRequiredService<IConfiguration>()));

        services.AddSingleton<MongoTweetStream>();
        services.AddSingleton(static (services) =>
        {
            return (SampleStreamV2)services.GetRequiredService<TwitterClient>().StreamsV2.CreateSampleStream();
        });

        services.AddTransient<ServiceResolver<ISampleStreamV2>>(serviceProvider => () =>
        {
            string implementationConfig = serviceProvider.GetRequiredService<IConfiguration>()["StreamProvider"];
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            ISampleStreamV2 imp = implementationConfig switch
            {
                "TwitterApi" => serviceProvider.GetRequiredService<SampleStreamV2>(),
                "MongoDb" => serviceProvider.GetRequiredService<MongoTweetStream>(),
                _ => throw new InvalidOperationException()
            };

            logger.LogWarning("using {}", imp.GetType());
            return imp;
        });


        services.AddSingleton<MongoNullInserter>();
        services.AddSingleton<MongoInserter>();
        services.AddTransient<ServiceResolver<IMongoInserter>>(serviceProvider => () =>
        {
            string implementationConfig = serviceProvider.GetRequiredService<IConfiguration>()["StreamProvider"];
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            IMongoInserter imp = implementationConfig switch
            {
                "TwitterApi" => serviceProvider.GetRequiredService<MongoInserter>(),
                "MongoDb" => serviceProvider.GetRequiredService<MongoNullInserter>(),
                _ => throw new InvalidOperationException()
            };

            logger.LogWarning("using {}", imp.GetType());
            return imp;
        });

        services.AddSingleton<Neo4JInserter>();
        services.AddSingleton(new TweetFilter().AllowLanguage("de", "en"));
    })
    .Build();

await host.RunAsync();


public delegate T ServiceResolver<T>();
