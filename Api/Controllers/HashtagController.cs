using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using Neo4jClient.Cypher;
using Shared;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HashtagController : ControllerBase
{
    private readonly ILogger<HashtagController> _logger;
    private readonly IGraphClient _neo4J;
    private const int MaxResultCount = 1000;

    public HashtagController(ILogger<HashtagController> logger, IGraphClient neo4J)
    {
        _logger = logger;
        _neo4J = neo4J;
    }

    [Route("{hashtag}/similar")]
    [HttpGet]
    public async Task<ActionResult> GetSimilar(string hashtag, string? language = null, int? lastDays = null)
    {
        if (string.IsNullOrWhiteSpace(hashtag))
            return BadRequest();

        var getTopics = _neo4J.Cypher.Read
            .Match("(h:Hashtag)-[:USED_IN]->(t:Tweet)<-[:USED_IN]-(h2:Hashtag)")
            .Where("h.Name = $p_name").WithParam("p_name", hashtag)
            .AndWhereDateTimeInLastDays("t.Date", lastDays)
            .AndWhereLangIs("t.Lang", language)
            .With("count(t) AS cnt, h2")
            .Return((h2, cnt) => new { Hashtag = h2.As<Hashtag>().Name })
            .OrderByDescending("cnt")
            .Limit(10);


        var topics = await getTopics.ResultsAsync;
        return Ok(topics);
    }

    [Route("{hashtag}/topics")]
    [HttpGet]
    public async Task<ActionResult> GetTopics(string hashtag, string? language = null)
    {
        if (string.IsNullOrWhiteSpace(hashtag))
            return BadRequest();

        var getTopics = _neo4J.Cypher.Read
            .Match("(h:Hashtag)")
            .Where((Hashtag h) => h.Name == hashtag)
            .Match("(h)-[:USED_IN]->(t:Tweet)<-[:MENTIONED_IN]-(e:Entity)")
            .Where("true") //needed to allow for AND without prior where
            .AndWhereLangIs("t.Lang", language)
            .With("count(t) AS cnt, e")
            .Return((e, cnt) => new { Topic = e.As<Entity>().Name })
            .OrderByDescending("cnt")
            .Limit(10);

        var topics = await getTopics.ResultsAsync;
        return Ok(topics);
    }

    [Route("top/{count:int?}")]
    [HttpGet]
    public async Task<ActionResult> GetTop(int count = 10, string? language = null, int? lastDays = null)
    {
        if (count < 0 || count > MaxResultCount)
            return BadRequest();

        var getHashtags = _neo4J.Cypher.Read
            .Match("(h:Hashtag)-[:USED_IN]->(t:Tweet) ")
            .Where("true") //needed to allow for AND without prior where
            .AndWhereLangIs("t.Lang", language)
            .AndWhereDateTimeInLastDays("t.Date", lastDays)
            .With("h, count(t) AS cnt")
            .Return((h, cnt) => new { Hashtag = h.As<Hashtag>().Name })
            .OrderByDescending("cnt")
            .Limit(count);

        var hashtags = await getHashtags.ResultsAsync;

        return Ok(hashtags);
    }

}

public static class Neo4jExtension
{
    private static DateTime? GetDateXDaysAgo(int? lastDays)
    {
        return lastDays is null ? null : DateTime.Today.AddDays(-1 * lastDays.Value);
    }

    public static ICypherFluentQuery AndWhereDateTimeInLastDays(this ICypherFluentQuery query, string property, int? lastDays)
    {
        DateTime? fromDate = GetDateXDaysAgo(lastDays);
        return query.AndWhereIf(lastDays.HasValue, property + " > $p_date").WithParam("p_date", fromDate);
    }

    public static ICypherFluentQuery AndWhereLangIs(this ICypherFluentQuery query, string property, string? language)
    {
        return query.AndWhereIf(language is not null, property + " = $p_lang").WithParam("p_lang", language);
    }
}
