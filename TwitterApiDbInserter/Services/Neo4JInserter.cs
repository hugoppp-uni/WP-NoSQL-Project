using Microsoft.Extensions.Logging;
using Neo4jClient;
using Shared;
using Tweetinvi.Models.V2;

namespace TwitterTest.Services;

public class Neo4JInserter
{
    private readonly Neo4JDataAccess _neo4JDataAccess;
    private readonly IGraphClient _graphClient;
    private readonly ILogger<Neo4JInserter> _logger;

    private HashSet<string> _alreadyAddedDomains = new();

    public Neo4JInserter(Neo4JDataAccess neo4JDataAccess, IGraphClient graphClient, ILogger<Neo4JInserter> logger)
    {
        _neo4JDataAccess = neo4JDataAccess;
        _graphClient = graphClient;
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

            await InsertAnnotations(tweetV2);

            await InsertHashtags(tweetV2);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "could not add tweet '{}'", tweetV2.Id);
        }
    }

    private async Task InsertHashtags(TweetV2 tweetV2)
    {
        IEnumerable<HashtagV2> hashtags = tweetV2.Entities.Hashtags ?? Enumerable.Empty<HashtagV2>();

        var uniqueHashtags = hashtags
            .DistinctBy(x => x.Tag)
            .Select(x => new Hashtag() { Name = x.Tag });

        //insert new hashtags
        foreach (var hashtag in uniqueHashtags)
        {
            await _graphClient.Cypher
                .Match("(tweet:Tweet {Id: $p_tweetId} )")
                .Merge("(n:Hashtag {Name: $p_name})")
                .WithParams(new { p_tweetId = tweetV2.Id, p_name = hashtag.Name })
                .OnCreate().Set("n.Count = 1")
                .OnMatch().Set("n.Count = n.Count + 1")
                .Create("(n) -[:USED_IN]-> (tweet)")
                .ExecuteWithoutResultsAsync();
        }
    }

    private async Task InsertAnnotations(TweetV2 tweetV2)
    {
        if (tweetV2.ContextAnnotations is null)
            return;

        //insert new domains
        IEnumerable<Domain> newDomains =
            tweetV2.ContextAnnotations
                .Select(x => x.Domain)
                .DistinctBy(x => x.Id)
                .Where(x => !_alreadyAddedDomains.Contains(x.Id))
                .Select(x => new Domain(x))
                .ToArray();
        newDomains.ForEach(domain => _alreadyAddedDomains.Add(domain.Id));
        await Task.WhenAll(newDomains.Select(_neo4JDataAccess.InsertAnnotationDomainIfNotExists));

        //insert new entities
        await Task.WhenAll(
            tweetV2.ContextAnnotations
                .Select(x => x.Entity)
                .DistinctBy(x => x.Id)
                .Select(x => _neo4JDataAccess.InsertAnnotationsEntityIfNotExists(new Entity(x)))
        );

        //relate domains and entities
        foreach (var annotation in tweetV2.ContextAnnotations)
        {
            await _graphClient.Cypher
                .Match("(tweet:Tweet {Id: $p_tweetId})").WithParam("p_tweetId", tweetV2.Id)
                .Match("(domain:Domain {Id: $p_domainId})").WithParam("p_domainId", annotation.Domain.Id)
                .Match("(entity:Entity {Name: $p_entityName})").WithParam("p_entityName", annotation.Entity.Name)
                .Create("(entity)-[:MENTIONED_IN]->(tweet)")
                .Merge("(entity)-[r:HAS_DOMAIN]->(domain)")
                .ExecuteWithoutResultsAsync();
        }
    }


}
