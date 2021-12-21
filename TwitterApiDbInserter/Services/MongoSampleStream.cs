using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared;
using Tweetinvi.Events;
using Tweetinvi.Events.V2;
using Tweetinvi.Models;
using Tweetinvi.Models.V2;
using Tweetinvi.Parameters.V2;
using Tweetinvi.Streaming.V2;

namespace TwitterTest.Services;

public class MongoSampleStream : ISampleStreamV2
{
    private readonly IMongoClient _mongoClient;
    private readonly ILogger<MongoSampleStream> _logger;
    private readonly IClientSessionHandle _session;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public MongoSampleStream(IMongoClient mongoClient, ILogger<MongoSampleStream> logger)
    {
        _mongoClient = mongoClient;
        _logger = logger;

        //todo
        var applicationCancellationToken = CancellationToken.None;

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(applicationCancellationToken);

        _session = mongoClient.StartSession(new ClientSessionOptions(), _cancellationTokenSource.Token);
    }

    public event EventHandler<StreamEventReceivedArgs>? EventReceived;

    public event EventHandler<TweetV2ReceivedEventArgs>? TweetReceived;

    public Task StartAsync(IStartSampleStreamV2Parameters parameters) => StartAsync();

    public Task StartAsync()
    {
        _logger.LogInformation($"Starting {nameof(MongoSampleStream)}");
        IMongoCollection<TweetV2> tweetCollection = _mongoClient.GetTweetCollection();
        IAsyncCursor<TweetV2>? allTweets = tweetCollection.Find(_session, x => true).ToCursor();

        int count = 0;
        return allTweets.ForEachAsync((tweet, index) =>
        {
            TweetReceived?.Invoke(this, new TweetV2ReceivedEventArgs(new TweetV2Response(){Tweet = tweet}, tweet.ToJson()));
            count++;
        }).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
                _logger.LogWarning("MongoDB Tweet stream completed (total count: {})", count);
            else
                _logger.LogCritical("MongoDB Tweet stream completed (total count: {})", count);
        });
    }

    public void StopStream()
    {
        _logger.LogInformation($"Stopping {nameof(MongoSampleStream)}");
        _cancellationTokenSource.Cancel();
        _logger.LogTrace($"{nameof(MongoSampleStream)} stopped");
    }

}
