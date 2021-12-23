using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared;
using Tweetinvi;
using Tweetinvi.Events.V2;
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
    private ISampleStreamV2 _sampleStreamV2;
    private Neo4JDataAccess _neo4J;

    public Worker(
        ILogger<Worker> logger,
        ITweetFilter tweetFilter,
        Neo4JInserter neo4JInserter,
        ServiceResolver<IMongoInserter> getMongoInserter,
        ServiceResolver<ISampleStreamV2> getSampleStream,
        Neo4JDataAccess neo4J)
    {
        _logger = logger;
        _neo4J = neo4J;

        _sampleStreamV2 = getSampleStream.Invoke();
        var mongoInserter = getMongoInserter.Invoke();

        _sampleStreamV2.TweetReceived += (_, args) =>
        {
            if (args.Tweet is null)
                return;

            _stats.ReceiveCount++;

            foreach (TweetV2 tweetToInsert in GetTweetsToInsert(args))
            {
                if (tweetFilter.TweetShouldBeIgnored(tweetToInsert))
                {
                    _stats.IgnoredTweetCount++;
                    continue;
                }

                Task.WhenAll(
                    mongoInserter.InsertTweetAsync(tweetToInsert),
                    neo4JInserter.InsertTweetAsync(tweetToInsert).ContinueWith((task) =>
                    {
                        if (task.Result) //aka inserted successfully
                            _stats.FilteredTweetCount++;
                        else
                            _stats.IgnoredTweetCount++;
                    })
                );
            }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //https://developer.twitter.com/en/docs/twitter-api/tweets/volume-streams/introduction
        TimeSpan rateLimitTimeSpan = TimeSpan.FromMinutes(15 / 50d);

        Task runStreamAndRetryIfFails = RunStreamAndRetryIfFails(stoppingToken, rateLimitTimeSpan * 1.2);

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan tick = TimeSpan.FromSeconds(20);
            _logger.LogInformation("Worker running at: {}, tick = {}s", DateTimeOffset.Now, tick.Seconds);

            int lastFilteredTweetCount = _stats.FilteredTweetCount;
            int lastReceiveCount = _stats.ReceiveCount;
            int lastIgnoredCount = _stats.IgnoredTweetCount;


            await Task.Delay(tick, stoppingToken);

            if (lastReceiveCount == _stats.ReceiveCount)
            {
                _logger.LogWarning("[Total] {}, no new events received since the last tick", _stats.ReceiveCount);
            }
            else
            {
                _logger.LogInformation("[Events] {} ({}/s)",
                    _stats.ReceiveCount,
                    (_stats.ReceiveCount - lastReceiveCount) / (double)tick.Seconds
                );
                _logger.LogInformation("[Filtered] {} ({}/s)",
                    _stats.FilteredTweetCount,
                    (_stats.FilteredTweetCount - lastFilteredTweetCount) / (double)tick.Seconds
                );
                _logger.LogInformation("[Ignored] {} ({}/s)",
                    _stats.IgnoredTweetCount,
                    (_stats.IgnoredTweetCount - lastIgnoredCount) / (double)tick.Seconds
                );
            }
        }

        _sampleStreamV2.StopStream();
        await runStreamAndRetryIfFails;
    }

    private async Task RunStreamAndRetryIfFails(CancellationToken stoppingToken, TimeSpan delay)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunStreamAsync();
                return; //<- stream successfully ended by other thread
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Stream was interrupted unexpectedly, retrying in {} seconds", delay.Seconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private Task RunStreamAsync()
    {
        return _sampleStreamV2.StartAsync(new StartSampleStreamV2Parameters()
        {
            Expansions =
            {
                TweetResponseFields.Expansions.AuthorId,
                TweetResponseFields.Expansions.ReferencedTweetsId,
                TweetResponseFields.Expansions.InReplyToUserId,
            },
            TweetFields =
            {
                TweetResponseFields.Tweet.AuthorId,
                TweetResponseFields.Tweet.ReferencedTweets,
                TweetResponseFields.Tweet.Id,
                // TweetResponseFields.Tweet.Text,
                TweetResponseFields.Tweet.Lang,
                TweetResponseFields.Tweet.CreatedAt,
                TweetResponseFields.Tweet.Entities,
                TweetResponseFields.Tweet.PossiblySensitive
            },
        });
    }

    private IEnumerable<TweetV2> GetTweetsToInsert(TweetV2ReceivedEventArgs args)
    {
        if (args.Tweet.ReferencedTweets is null || args.Tweet.ReferencedTweets.Length == 0)
        {
            yield return args.Tweet;

            yield break;
        }

        bool isRetweet = false;
        foreach (ReferencedTweetV2 referencedTweet in args.Tweet.ReferencedTweets)
        {
            if (referencedTweet.Type() == TweetType.Retweet)
            {
                isRetweet = true;
            }

            //return all the referenced tweets
            TweetV2? original = args.Includes?.Tweets?.FirstOrDefault(x => x.Id == referencedTweet.Id);
            if (original is not null)
                yield return original;
        }

        //we don't want the retweet as it is pretty much just a copy
        if (!isRetweet)
            yield return args.Tweet;
    }

}
