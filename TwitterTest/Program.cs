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

        Task<GraphClient> graphClient = ConnectToNeo4JAsync();

        services.AddSingleton<IGraphClient>(graphClient.Result);
        services.AddSingleton<Neo4JInserter>();
        services.AddSingleton<TweetLanguageAnalyzer>();
        services.AddSingleton(new TweetFilter().AllowLanguage("de", "en"));

    })
    .Build();

await host.RunAsync();

async Task<GraphClient> ConnectToNeo4JAsync()
{
    GraphClient client = new GraphClient(new Uri("http://localhost:7474"), "neo4j","twitter");
    await client.ConnectAsync();
    return client;
}
