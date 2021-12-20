using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tweetinvi;
using Tweetinvi.Core.Iterators;
using Tweetinvi.Models;
using Tweetinvi.Models.V2;
using Tweetinvi.Parameters.V2;
using Tweetinvi.Streaming.V2;
using TwitterTest.Models;
using TwitterTest.Services;

namespace TwitterTest;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private Stats _stats = new();
    private TweetLanguageAnalyzer _tweetLanguageAnalyzer;
    private ISampleStreamV2 _sampleStreamV2;

    public Worker(ILogger<Worker> logger, IConfiguration config, TweetFilter tweetFilter, TweetLanguageAnalyzer tweetLanguageAnalyzer,
        Neo4JInserter neo4JInserter)
    {
        _logger = logger;
        _tweetLanguageAnalyzer = tweetLanguageAnalyzer;


        var userClient = new TwitterClient(CreateCredentials(config));


        _sampleStreamV2 = userClient.StreamsV2.CreateSampleStream();

        _sampleStreamV2.TweetReceived += (_, x) =>
        {
            if (x.Tweet is null)
                return;

            tweetLanguageAnalyzer.AddTweet(x.Tweet);
            _stats.TotalTweetCount++;
            if (tweetFilter.TweetShouldBeIgnored(x.Tweet))
                return;

            lock (this)
                neo4JInserter.InsertTweetAsync(x.Tweet).Wait();

            _stats.FilteredTweetCount++;
        };

        _sampleStreamV2.StartAsync(new StartSampleStreamV2Parameters()
        {
            // Expansions =
            // {
            //     TweetResponseFields.Expansions.AuthorId,
            //     TweetResponseFields.Expansions.ReferencedTweetsId,
            //     TweetResponseFields.Expansions.InReplyToUserId,
            // },
            // TweetFields =
            // {
            //     TweetResponseFields.Tweet.AuthorId,
            //     TweetResponseFields.Tweet.ReferencedTweets,
            //     TweetResponseFields.Tweet.Id,
            //     TweetResponseFields.Tweet.Text,
            //     TweetResponseFields.Tweet.Lang,
            //     TweetResponseFields.Tweet.CreatedAt,
            //     TweetResponseFields.Tweet.Entities,
            //     TweetResponseFields.Tweet.,
            // }
        }).ContinueWith(x =>
        {
            if (!x.IsCompletedSuccessfully)
            {
                logger.LogError("Stream stopped, success: {}", x.IsCompletedSuccessfully);
                throw new Exception("Stream was interrupted unexpectedly", x.Exception);
            }
        });
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            int lastFilteredTweetCount = _stats.FilteredTweetCount;
            int lastTotalTweetCount = _stats.TotalTweetCount;


            const int millisecondsDelay = 5000;
            await Task.Delay(millisecondsDelay, stoppingToken);

            _logger.LogInformation("[Total] {} ({}/s)",
                _stats.TotalTweetCount,
                (_stats.TotalTweetCount - lastTotalTweetCount) / (millisecondsDelay / 1000d)
            );
            _logger.LogInformation("[Filtered] {} ({}/s)",
                _stats.FilteredTweetCount,
                (_stats.FilteredTweetCount - lastFilteredTweetCount) / (millisecondsDelay / 1000d)
            );

            string languageStats = string.Join(Environment.NewLine,
                _tweetLanguageAnalyzer.TweetsPerLanguageString()
                    .Take(10)
                    .Prepend("Language stats:"));

            _logger.LogInformation(languageStats);

            Console.WriteLine();
        }

        _sampleStreamV2.StopStream();
    }

    private static IReadOnlyConsumerCredentials CreateCredentials(IConfiguration config)
    {
        string bearerToken = config["TWITTER_BEARER_TOKEN"] ?? throw new InvalidOperationException($"Configuration is missing bearerToken");
        string consumerSecret = config["TWITTER_CONSUMER_SECRET"] ??
                                throw new InvalidOperationException($"Configuration is missing consumerSecret");
        string consumerKey = config["TWITTER_CONSUMER_KEY"] ?? throw new InvalidOperationException($"Configuration is missing consumerKey");

        return new ReadOnlyConsumerCredentials(consumerKey, consumerSecret, bearerToken);
    }
}
