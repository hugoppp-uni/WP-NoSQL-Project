using MongoDB.Driver;
using Shared;
using Tweetinvi.Models.V2;

namespace TwitterTest.Services;

public interface IMongoInserter
{
    Task InsertTweetAsync(TweetV2 tweet);
}

public class MongoNullInserter : IMongoInserter
{

    public Task InsertTweetAsync(TweetV2 tweet)
    {
        return Task.CompletedTask;
    }

}

public class MongoInserter : IMongoInserter
{
    private IMongoCollection<TweetV2> _tweetCollection;

    public MongoInserter(IMongoClient mongoDatabase)
    {
        _tweetCollection = mongoDatabase.GetTweetCollection();
    }

    public Task InsertTweetAsync(TweetV2 tweet)
    {
        try
        {
            return _tweetCollection.InsertOneAsync(tweet);
        }
        catch (Exception e)
        {
            //todo: this sucks, maybe we should check for dubs instead
            return Task.CompletedTask;
        }
    }
}
