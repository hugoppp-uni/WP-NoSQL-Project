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

    public async Task InsertTweetAsync(TweetV2 tweetV2)
    {
        try
        {
            ReferencedTweetV2? retweetOriginal =
                tweetV2.ReferencedTweets?.FirstOrDefault(x => x.Type() == TweetType.Retweet);
            bool isRetweet = retweetOriginal is not null;

            await _neo4JDataAccess.InsertTweet(new Tweet()
            {
                AuthorId = isRetweet ? null : tweetV2.AuthorId,
                Id = isRetweet ? retweetOriginal!.Id : tweetV2.Id,
                IsCreatedFromRetweet = isRetweet,
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
        catch (Exception e)
        {
            _logger.LogError(e, "could not add tweet '{}'", tweetV2.Id);
        }
    }



}
