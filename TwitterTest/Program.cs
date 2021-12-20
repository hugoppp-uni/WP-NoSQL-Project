using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neo4jClient;
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

        services.AddSingleton<MongoInserter>();
        services.AddSingleton<Neo4JInserter>();
        services.AddSingleton(new TweetFilter().AllowLanguage("de", "en"));
    })
    .Build();

await host.RunAsync();

