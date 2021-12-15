using Microsoft.Extensions.Configuration;
using Tweetinvi;
using Tweetinvi.Core.Extensions;
using Tweetinvi.Models;
using Tweetinvi.Parameters.V2;
using Tweetinvi.Streaming.V2;
using TwitterTest;

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var userClient = new TwitterClient(CreateCredentials(config));


ISampleStreamV2 sampleStreamV2 = userClient.StreamsV2.CreateSampleStream();

Stats stats = new();
TweetLanguageAnalyzer tweetLanguageAnalyzer = new();
TweetFilter filter = new TweetFilter().AllowLanguage("de", "en");

sampleStreamV2.TweetReceived += (_, x) =>
{
    if (x.Tweet is null)
        return;

    tweetLanguageAnalyzer.AddTweet(x.Tweet);
    stats.TotalTweetCount++;
    if (filter.TweetShouldBeIgnored(x.Tweet))
        return;

    stats.FilteredTweetCount++;
};

//do not await here, as the task completes when the stream ends
_ = sampleStreamV2.StartAsync(new StartSampleStreamV2Parameters());

while (true)
{
    int lastFilteredTweetCount = stats.FilteredTweetCount;
    int lastTotalTweetCount = stats.TotalTweetCount;


    const int millisecondsDelay = 5000;
    await Task.Delay(millisecondsDelay);

    Console.WriteLine($"[Total] {stats.TotalTweetCount} ({(stats.TotalTweetCount - lastTotalTweetCount) / (millisecondsDelay / 1000d)}/s)");
    Console.WriteLine(
        $"[Filtered] {stats.FilteredTweetCount} ({(stats.FilteredTweetCount - lastFilteredTweetCount) / (millisecondsDelay / 1000d)}/s)");

    tweetLanguageAnalyzer
        .TweetsPerLanguageString()
        .Take(10)
        .ForEach(Console.WriteLine);

    Console.WriteLine();
}

static IReadOnlyConsumerCredentials CreateCredentials(IConfiguration config)
{
    string bearerToken = config["TWITTER_BEARER_TOKEN"] ?? throw new InvalidOperationException($"Configuration is missing bearerToken");
    string consumerSecret = config["TWITTER_CONSUMER_SECRET"] ??
                            throw new InvalidOperationException($"Configuration is missing consumerSecret");
    string consumerKey = config["TWITTER_CONSUMER_KEY"] ?? throw new InvalidOperationException($"Configuration is missing consumerKey");

    return new ReadOnlyConsumerCredentials(consumerKey, consumerSecret, bearerToken);
}
