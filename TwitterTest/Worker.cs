using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters.V2;
using Tweetinvi.Streaming.V2;
using TwitterTest.Models;
using TwitterTest.Services;

namespace TwitterTest;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private Stats _stats = new();
    private ISampleStreamV2 _sampleStreamV2;

    public Worker(
        ILogger<Worker> logger,
        TweetFilter tweetFilter,
        Neo4JInserter neo4JInserter,
        ServiceResolver<IMongoInserter> getMongoInserter,
        ServiceResolver<ISampleStreamV2> getSampleStream)
    {
        _logger = logger;

        _sampleStreamV2 = getSampleStream.Invoke();
        var mongoInserter = getMongoInserter.Invoke();

        _sampleStreamV2.TweetReceived += (_, x) =>
        {
            if (x.Tweet is null)
                return;

            _stats.TotalTweetCount++;
            if (tweetFilter.TweetShouldBeIgnored(x.Tweet))
                return;

            Task insertMongo = mongoInserter.InsertTweetAsync(x.Tweet);
            Task insertNeo4J = neo4JInserter.InsertTweetAsync(x.Tweet);

            Task.WhenAll(insertMongo, insertNeo4J).Wait();

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
                logger.LogCritical("Stream was interrupted unexpectedly");
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
        }

        _sampleStreamV2.StopStream();
    }
}
