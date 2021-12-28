using Neo4j.Driver;
using Neo4jClient;
using Neo4jClient.Cypher;
using Tweetinvi.Models.V2;

namespace Shared;

public class Neo4JDataAccess
{
    private IGraphClient _graphClient;
    private HashSet<string> _alreadyAddedDomains = new();

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

    public Task InsertHashtagsAndRelateWithTweet(Hashtag hashtag, string tweetId)
    {
        ICypherFluentQuery cypherFluentQuery = _graphClient.Cypher
            .Match("(tweet:Tweet {Id: $p_tweetId} )")
            .Merge("(n:Hashtag {Name: $p_name})")
            .WithParams(new { p_tweetId = tweetId, p_name = hashtag.Name })
            .OnCreate().Set("n.Count = 1")
            .OnMatch().Set("n.Count = n.Count + 1")
            .Create("(n) -[:USED_IN]-> (tweet)");
        return cypherFluentQuery
            .ExecuteWithoutResultsAsync();
    }

    public async Task InsertAnnotationsAndRelateWithTweet(IList<TweetContextAnnotationV2> tweetV2ContextAnnotations, string tweetId)
    {
        //insert domains
        IEnumerable<Domain> newDomains =
            tweetV2ContextAnnotations
                .Select(x => x.Domain)
                .DistinctBy(x => x.Id)
                .Where(x => !_alreadyAddedDomains.Contains(x.Id))
                .Select(x => new Domain(x))
                .ToArray();
        newDomains.ForEach(domain => _alreadyAddedDomains.Add(domain.Id));
        IEnumerable<Task> insertDomainTasks = newDomains.Select(InsertAnnotationDomainIfNotExists);

        //insert entities
        IEnumerable<Task> insertEntitiesTasks = tweetV2ContextAnnotations
            .Select(x => x.Entity)
            .DistinctBy(x => x.Id)
            .Select(x => InsertAnnotationsEntityIfNotExists(new Entity(x)));

        //wait for booth of them to complete
        await Task.WhenAll(
            insertEntitiesTasks.Concat(insertDomainTasks)
        );

        //then relate domains, entities and tweets
        foreach (var annotation in tweetV2ContextAnnotations)
        {
            //https://neo4j.com/docs/java-reference/current/transaction-management/#transactions-deadlocks
            const int maxRetryCount = 3;

            for (int retryCount = 0;; retryCount++)
            {
                try
                {
                    await Relate(annotation);
                    break;
                }
                catch (ClientException e) when (e.Code == "Neo.TransientError.Transaction.DeadlockDetected")
                {
                    if (retryCount >= maxRetryCount)
                        throw;

                    TimeSpan backoff = TimeSpan.FromMilliseconds(5);
                    await Task.Delay(backoff);
                }
            }
        }

        async Task Relate(TweetContextAnnotationV2 annotation)
        {
            await _graphClient.Cypher
                .Match("(tweet:Tweet {Id: $p_tweetId})").WithParam("p_tweetId", tweetId)
                .Match("(domain:Domain {Id: $p_domainId})").WithParam("p_domainId", annotation.Domain.Id)
                .Match("(entity:Entity {Name: $p_entityName})").WithParam("p_entityName", annotation.Entity.Name)
                .Create("(entity)-[:MENTIONED_IN]->(tweet)")
                .Merge("(entity)-[r:HAS_DOMAIN]->(domain)")
                .ExecuteWithoutResultsAsync();
        }
    }

    private Task InsertAnnotationsEntityIfNotExists(Entity entity)
    {
        ICypherFluentQuery cypherFluentQuery = _graphClient.Cypher
            .Merge("(e:Entity {Id: $Id})")
            .OnCreate().Set("e.Name = $Name")
            .WithParams(entity);
        return cypherFluentQuery
            .ExecuteWithoutResultsAsync();
    }

    private Task InsertAnnotationDomainIfNotExists(Domain domain)
    {
        ICypherFluentQuery cypherFluentQuery = _graphClient.Cypher
            .Merge("(d:Domain {Id: $Id})")
            .OnCreate().Set(
                "d.Name = $Name, " +
                "d.Description = $Description")
            .WithParams(domain);
        return cypherFluentQuery
            .ExecuteWithoutResultsAsync();
    }

}
