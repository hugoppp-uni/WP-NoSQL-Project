using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TwitterTest;
using TwitterTest.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
        config.AddEnvironmentVariables())
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();

        services.AddSingleton(new TweetFilter().AllowLanguage("de", "en"));
        services.AddSingleton<TweetLanguageAnalyzer>();
    })
    .Build();

await host.RunAsync();
