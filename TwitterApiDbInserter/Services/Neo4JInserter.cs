using Microsoft.Extensions.Logging;
using Shared;
using Tweetinvi.Models.V2;

namespace TwitterTest.Services;

public class Neo4JInserter
{
    private readonly Neo4JDataAccess _neo4JDataAccess;
    private readonly ILogger<Neo4JInserter> _logger;


    public Neo4JInserter(Neo4JDataAccess neo4JDataAccess, ILogger<Neo4JInserter> logger)
    {
        _neo4JDataAccess = neo4JDataAccess;
        _logger = logger;
    }

    public async Task<bool> InsertTweetAsync(TweetV2 tweetV2)
    {
        if (await _neo4JDataAccess.TweetWithIdExists(tweetV2.Id))
            return false;

        try
        {
            await InsertTweetInternalAsync(tweetV2);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "could not add tweet '{}'", tweetV2.Id);
            return false;
        }

        return true;
    }

    private async Task InsertTweetInternalAsync(TweetV2 tweetV2)
    {
        await _neo4JDataAccess.InsertTweet(new Tweet()
        {
            AuthorId = tweetV2.AuthorId,
            Id = tweetV2.Id,
            IsCreatedFromRetweet = false,
            Lang = tweetV2.Lang,
            Sensitive = tweetV2.PossiblySensitive,
            Date = tweetV2.CreatedAt.Date, // not correct when retweet, but we don't care, the retweet date is close enough
        });

        if (tweetV2.ContextAnnotations is not null)
            await _neo4JDataAccess.InsertAnnotationsAndRelateWithTweet(tweetV2.ContextAnnotations, tweetV2.Id);

        if (tweetV2.Entities.Hashtags is not null)
        {
            IEnumerable<Task> insertUniqueHashtagsTasks = tweetV2.Entities.Hashtags
                .DistinctBy(x => x.Tag)
                .Select(x => new Hashtag() { Name = x.Tag })
                .Select(x => _neo4JDataAccess.InsertHashtagsAndRelateWithTweet(x, tweetV2.Id));

            await Task.WhenAll(insertUniqueHashtagsTasks);
        }
    }


}
