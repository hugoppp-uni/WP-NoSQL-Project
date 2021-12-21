using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
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

        string date = DateTime.Today.DateOnlyXDaysAgo(lastDays);

        var getTopics = _neo4J.Cypher.Read
            .Match("(h:Hashtag)-[:USED_IN]->(t:Tweet)<-[:USED_IN]-(h2:Hashtag)")
            .Where("h.Name = $p_name").WithParam("p_name", hashtag)
            .AndWhereIf(lastDays.HasValue, "t.Date > date($p_date)").WithParam("p_date", date)
            .AndWhereIf(language is not null, "t.Lang = $lang").WithParam("lang", language)
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
            .WhereIf(language is not null, "t.Lang = $lang").WithParam("lang", language)
            .With("count(t) AS cnt, e")
            .Return((e, cnt) => new { Topic = e.As<Entity>().Name })
            .OrderByDescending("cnt")
            .Limit(10);

        var topics = await getTopics.ResultsAsync;
        return Ok(topics);
    }

    [Route("top/{count:int?}")]
    [HttpGet]
    public async Task<ActionResult> GetTop(int count = 10, string? language = null)
    {
        if (count < 0 || count > MaxResultCount)
            return BadRequest();

        var getHashtags = _neo4J.Cypher.Read
            .Match("(h:Hashtag)-[:USED_IN]->(t:Tweet) ")
            .WhereIf(language is not null, "t.Lang = $lang").WithParam("lang", language)
            .With("h, count(t) AS cnt")
            .Return((h, cnt) => new { Hashtag = h.As<Hashtag>().Name })
            .OrderByDescending("cnt")
            .Limit(count);

        var hashtags = await getHashtags.ResultsAsync;

        return Ok(hashtags);
    }
}
