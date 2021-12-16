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
        if (hashtags is null)
            return;

        var uniqueHashtags = hashtags
            .GroupBy(x => x.Tag)
            .Select(x => new { Name = x.Key, Count = x.Count() })
            .ToArray();

        foreach (var hashtag in uniqueHashtags)
        {
            await _graphClient.Cypher
                .Merge("(n:Hashtag {name: $p_name})")
                .WithParam("p_name", hashtag.Name)
                .OnCreate().Set($"n.count = {hashtag.Count}")
                .OnMatch().Set($"n.count = n.count + {hashtag.Count}")
                .ExecuteWithoutResultsAsync();
        }


        var kCombinations = uniqueHashtags.Select(x => x.Name).GetKCombinations(2);

        foreach (string[] kCombination in kCombinations.Select(x => x.ToArray()))
        {
            _logger.LogInformation("Adding '{}' -> '{}'", kCombination[0], kCombination[1]);
            await _graphClient.Cypher
                .Match("(n0:Hashtag {name: $p_name0})")
                .WithParam("p_name0", kCombination[0])
                .Match("(n1:Hashtag {name: $p_name1})")
                .WithParam("p_name1", kCombination[1])
                .Merge("(n0)-[r:RELATES]-(n1)")
                .OnCreate().Set("r.count = 1")
                .OnMatch().Set("r.count = r.count + 1")
                .ExecuteWithoutResultsAsync();
        }
    }
}
