using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Neo4jClient;
using Tweetinvi.Core.Events;
using Tweetinvi.Core.Extensions;
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
        if (tweetV2.ReferencedTweets?.Any(x => x.Type == "retweeted") ?? false)
            return;
        if (tweetV2.Entities?.Hashtags is null)
            return;


        await InsertTweet(tweetV2);

        await InsertAnnotations(tweetV2);

        await InsertHashtags(tweetV2);
    }

    private async Task InsertHashtags(TweetV2 tweetV2)
    {
        var hashtags = tweetV2.Entities.Hashtags;
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
                .WithParam("p_name", hashtag.Name)
                .OnCreate().Set($"n.count = {hashtag.Count}")
                .OnMatch().Set($"n.count = n.count + {hashtag.Count}")
                .WithParams(new { p_tweetId = tweetV2.Id })
                .Create("(n) -[:USED_IN]-> (tweet)")
                .ExecuteWithoutResultsAsync();
        }
    }

    private async Task InsertAnnotations(TweetV2 tweetV2)
    {
        var annotationEntities = tweetV2.GetDistinctContextAnnotationEntities().ToArray();

        foreach (TweetContextAnnotationEntityV2 annotationEntity in annotationEntities)
        {
            await _graphClient.Cypher
                .Match("(tweet:Tweet {id: $p_tweetId})")
                .WithParam("p_tweetId", tweetV2.Id)
                .Merge("(entity:AnnotationEntity {name: $p_name})")
                .WithParam("p_name", annotationEntity.Name)
                .OnCreate().Set("entity.count = 1")
                .OnMatch().Set("entity.count = entity.count + 1")
                .Create("(entity)-[:MENTIONED_IN]->(tweet)")
                .ExecuteWithoutResultsAsync();
        }
    }

    private async Task InsertTweet(TweetV2 tweetV2)
    {
        await _graphClient.Cypher
            .Create("(:Tweet {id:$p_id, text:$p_text, lang:$p_lang, date:date($p_date)})")
            .WithParams(new
            {
                p_id = tweetV2.Id,
                p_text = tweetV2.Text,
                p_lang = tweetV2.Lang,
                p_date = tweetV2.CreatedAt.Date.ToString("yyyy-MM-dd")
            })
            .ExecuteWithoutResultsAsync();
    }


}
