using Neo4jClient;

namespace Shared;

public class Neo4JQueries
{
    public static async Task<bool> TweetExists(IGraphClient graphClient, string id)
    {
        var originalTweetExists = graphClient.Cypher.Read
            .Match("(t:Tweet {Id: $p_id})").WithParam("p_id", id)
            .Return<int>("COUNT(t)");

        int count = (await originalTweetExists.ResultsAsync).First();

        return count > 0;
    }

}
