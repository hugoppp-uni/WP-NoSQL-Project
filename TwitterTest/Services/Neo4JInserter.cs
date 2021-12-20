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
            await InsertHashTags(tweetV2);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "could not add tweet '{}'", tweetV2.Id);
        }
    }

    private async Task InsertHashTags(TweetV2 tweetV2)
    {
        HashtagV2[] hashtags = tweetV2.Entities.Hashtags;
        if (tweetV2.ReferencedTweets?.Any(x => x.Type == "retweeted") ?? false)
            return;
        if (hashtags is null)
            return;

        var uniqueHashtags = hashtags
            .GroupBy(x => x.Tag)
            .Select(x => new { Name = x.Key, Count = x.Count() })
            .ToArray();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            string hashtagsString =
                string.Join(", ",
                    uniqueHashtags
                        .OrderByDescending(x => x.Count)
                        .Select(x => $"{x.Name} ({x.Count})")
                );
            _logger.LogInformation("adding hashtags: {}", hashtagsString);
        }

        await _graphClient.Cypher
            .Create("(:Tweet {id:$p_id, text:$p_text, lang:$p_lang})")
            .WithParams(new { p_id = tweetV2.Id, p_text = tweetV2.Text, p_lang = tweetV2.Lang})
            .ExecuteWithoutResultsAsync();

        var annotationEntities = tweetV2.GetDistinctContextAnnotationEntities().ToArray();
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

            foreach (TweetContextAnnotationEntityV2 annotationEntity in annotationEntities)
            {
                await _graphClient.Cypher
                    .Match("(hashtag:Hashtag {name: $p_hashtag})")
                    .WithParam("p_hashtag", hashtag.Name)
                    .Merge("(entity:AnnotationEntity {name: $p_name})")
                    .OnCreate().Set("entity.count = 1")
                    .OnMatch().Set("entity.count = entity.count + 1")
                    .WithParam("p_name", annotationEntity.Name)
                    .Create("(entity)<-[:HAS_ENTITY]-(hashtag)")
                    .ExecuteWithoutResultsAsync();
            }
        }


        var kCombinations = uniqueHashtags.Select(x => x.Name).GetKCombinations(2);

        foreach (string[] kCombination in kCombinations.Select(x => x.ToArray()))
        {
            // _logger.LogInformation("Adding '{}' -> '{}'", kCombination[0], kCombination[1]);
            await _graphClient.Cypher
                .Match("(n0:Hashtag {name: $p_name0})")
                .WithParam("p_name0", kCombination[0])
                .Match("(n1:Hashtag {name: $p_name1})")
                .WithParam("p_name1", kCombination[1])
                .Merge("(n0)-[r:RELATES {" +
                       "day: date($p_date)" +
                       "} ]-(n1)")
                .WithParam("p_date", tweetV2.CreatedAt.Date.ToString("yyyy-MM-dd"))
                .OnCreate().Set("r.count = 1")
                .OnMatch().Set("r.count = r.count + 1")
                .ExecuteWithoutResultsAsync();
        }
    }


}
