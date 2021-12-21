using Microsoft.Extensions.Logging;
using Neo4jClient;
using Tweetinvi.Models.V2;

namespace TwitterTest.Services;

public class Neo4JInserter
{
    private readonly IGraphClient _graphClient;
    private readonly ILogger<Neo4JInserter> _logger;

    private HashSet<string> _alreadyAddedDomains = new();

    public Neo4JInserter(IGraphClient graphClient, ILogger<Neo4JInserter> logger)
    {
        _graphClient = graphClient;
        _logger = logger;
    }

    public async Task InsertTweetAsync(TweetV2 tweetV2)
    {
        try
        {
            await Insert(tweetV2);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "could not add tweet '{}'", tweetV2.Id);
        }
    }

    private async Task Insert(TweetV2 tweetV2)
    {
        await InsertTweet(tweetV2);

        await InsertAnnotations(tweetV2);

        await InsertHashtags(tweetV2);
    }

    private async Task InsertHashtags(TweetV2 tweetV2)
    {
        IEnumerable<HashtagV2> hashtags = tweetV2.Entities.Hashtags ?? Enumerable.Empty<HashtagV2>();

        var uniqueHashtags = hashtags
            .GroupBy(x => x.Tag)
            .Select(x => new { Name = x.Key, Count = x.Count() })
            .ToArray();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            string hashtagsString = string.Join(", ",
                uniqueHashtags
                    .OrderByDescending(x => x.Count)
                    .Select(x => $"{x.Name} ({x.Count})")
            );
            _logger.LogTrace("adding hashtags: {}", hashtagsString);
        }

        foreach (var hashtag in uniqueHashtags)
        {
            await _graphClient.Cypher
                .Match("(tweet:Tweet {id: $p_tweetId} )")
                .Merge("(n:Hashtag {name: $p_name})")
                .WithParams(new { p_tweetId = tweetV2.Id, p_name = hashtag.Name })
                .OnCreate().Set("n.count = 1")
                .OnMatch().Set("n.count = n.count + 1")
                .Create("(n) -[:USED_IN]-> (tweet)")
                .ExecuteWithoutResultsAsync();
        }
    }

    private async Task InsertAnnotations(TweetV2 tweetV2)
    {
        if (tweetV2.ContextAnnotations is null)
            return;

        //insert new domains
        await Task.WhenAll(tweetV2.ContextAnnotations
            .Select(x => x.Domain)
            .DistinctBy(x => x.Id)
            .Where(x => !_alreadyAddedDomains.Contains(x.Id))
            .Select(InsertAnnotationDomain));

        //insert new entities
        await Task.WhenAll(tweetV2.ContextAnnotations
            .Select(x => x.Entity)
            .DistinctBy(x => x.Id)
            .Select(InsertAnnotationsEntity));

        foreach (var annotation in tweetV2.ContextAnnotations)
        {
            await _graphClient.Cypher
                .Match("(tweet:Tweet {id: $p_tweetId})").WithParam("p_tweetId", tweetV2.Id)
                .Match("(domain:Domain {id: $p_domainId})").WithParam("p_domainId", annotation.Domain.Id)
                .Match("(entity:Entity {name: $p_entityName})").WithParam("p_entityName", annotation.Entity.Name)
                .Create("(entity)-[:MENTIONED_IN]->(tweet)")
                .Merge("(entity)-[r:HAS_DOMAIN]->(domain)")
                .ExecuteWithoutResultsAsync();
        }

        Task InsertAnnotationsEntity(TweetContextAnnotationEntityV2 annotationEntity)
        {
            return _graphClient.Cypher
                .Merge("(e:Entity {id: $p_id})")
                .OnCreate().Set("e.name = $p_name")
                .WithParams(new
                {
                    p_id = annotationEntity.Id,
                    p_name = annotationEntity.Name,
                })
                .ExecuteWithoutResultsAsync();
        }

        Task InsertAnnotationDomain(TweetContextAnnotationDomainV2 domain)
        {
            _alreadyAddedDomains.Add(domain.Id);
            return _graphClient.Cypher
                .Merge("(d:Domain {id: $p_id})")
                .OnCreate().Set("d.name = $p_name, d.description = $p_description")
                .WithParams(new
                {
                    p_name = domain.Name ?? "",
                    p_description = domain.Description ?? "",
                    p_id = domain.Id,
                })
                .ExecuteWithoutResultsAsync();
        }
    }

    private Task InsertTweet(TweetV2 tweetV2)
    {
        return _graphClient.Cypher
            .Create("( :Tweet {" +
                    "id:$p_id," +
                    "text:$p_text," +
                    "lang:$p_lang," +
                    "date:date($p_date)," +
                    "authorId:$p_authorId," +
                    "sensitive:$p_sensitive" +
                    "})")
            .WithParams(new
            {
                p_sensitive = tweetV2.PossiblySensitive,
                p_id = tweetV2.Id,
                p_text = tweetV2.Text,
                p_lang = tweetV2.Lang,
                p_date = tweetV2.CreatedAt.Date.ToString("yyyy-MM-dd"),
                p_authorId = tweetV2.AuthorId,
            })
            .ExecuteWithoutResultsAsync();
    }


}
