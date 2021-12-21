using Microsoft.Extensions.Logging;
using Neo4jClient;
using Tweetinvi.Models.V2;

namespace TwitterTest.Services;

public class Neo4JInserter
{
    private readonly IGraphClient _graphClient;
    private readonly ILogger<Neo4JInserter> _logger;

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
        IEnumerable<TweetContextAnnotationV2> contextAnnotations =
            tweetV2.ContextAnnotations ?? Enumerable.Empty<TweetContextAnnotationV2>();

        foreach (var annotation in contextAnnotations)
        {
            await InsertAnnotationDomain(annotation.Domain);
            await InsertAnnotationsEntity(annotation.Entity);

            await _graphClient.Cypher
                .Match("(tweet:Tweet {id: $p_tweetId})").WithParam("p_tweetId", tweetV2.Id)
                .Match("(domain:Domain {id: $p_domainId})").WithParam("p_domainId", annotation.Domain.Id)
                .Match("(entity:Entity {name: $p_entityName})").WithParam("p_entityName", annotation.Entity.Name)
                .Create("(entity)-[:MENTIONED_IN]->(tweet)")
                .Merge("(entity)-[r:HAS_DOMAIN]->(domain)")
                .ExecuteWithoutResultsAsync();
        }

        async Task InsertAnnotationsEntity(TweetContextAnnotationEntityV2 annotationEntity)
        {
            await _graphClient.Cypher
                .Merge("(e:Entity {name: $p_name})")
                .WithParams(new
                {
                    p_id = annotationEntity.Id,
                    p_name = annotationEntity.Name,
                })
                .ExecuteWithoutResultsAsync();
        }

        Task InsertAnnotationDomain(TweetContextAnnotationDomainV2 domain)
        {
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

    private async Task InsertTweet(TweetV2 tweetV2)
    {
        await _graphClient.Cypher
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
