using Neo4jClient;
using Neo4jClient.Cypher;

namespace Shared;

public class Neo4JDataAccess
{
    private IGraphClient _graphClient;


    public Neo4JDataAccess(IGraphClient graphClient)
    {
        _graphClient = graphClient;
    }

    public async Task<bool> TweetWithIdExists(string id)
    {
        var originalTweetExists = _graphClient.Cypher.Read
            .Match("(t:Tweet {Id: $p_id})").WithParam("p_id", id)
            .Return<int>("COUNT(t)");

        int count = (await originalTweetExists.ResultsAsync).First();

        return count > 0;
    }

    public Task InsertTweet(Tweet tweet)
    {
        ICypherFluentQuery cypherFluentQuery = _graphClient.Cypher
            .Create("( t:Tweet $newTweet )")
            .WithParam("newTweet", tweet);

        return cypherFluentQuery
            .ExecuteWithoutResultsAsync();
    }

    public Task InsertAnnotationsEntityIfNotExists(Entity entity)
    {
        ICypherFluentQuery cypherFluentQuery = _graphClient.Cypher
            .Merge("(e:Entity {Id: $Id})")
            .OnCreate().Set("e.Name = $Name")
            .WithParams(entity);
        return cypherFluentQuery
            .ExecuteWithoutResultsAsync();
    }

    public Task InsertAnnotationDomainIfNotExists(Domain domain)
    {
        return _graphClient.Cypher
            .Merge("(d:Domain {Id: $Id})")
            .OnCreate().Set(
                "d.Name = $Name, " +
                "d.Description = $Description")
            .WithParams(domain)
            .ExecuteWithoutResultsAsync();
    }
}
