using Neo4jClient;
using Tweetinvi.Models.V2;

namespace Shared;

public record Tweet
{
    public bool Sensitive { get; set; }

    public string Id { get; set; }

    public string Lang { get; set; }

    [Neo4jDateTime] public DateTime Date { get; set; }

    public string? AuthorId { get; set; }

    public bool IsCreatedFromRetweet { get; set; }
}

public record Hashtag
{
    public string Name { get; set; }

    public int Count { get; set; }

}

public record Domain
{
    public string Name { get; set; }

    public string Id { get; set; }

    public string Description { get; set; }

    public Domain(TweetContextAnnotationDomainV2 domain)
    {
        Name = domain.Name;
        Id = domain.Id;
        Description = domain.Description;
    }

    public Domain()
    {
    }

}

public record Entity
{
    public string Name { get; set; }

    public Entity()
    {
    }

    public Entity(TweetContextAnnotationEntityV2 entity)
    {
        Name = entity.Name;
    }

}
