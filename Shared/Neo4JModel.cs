using Neo4jClient;

namespace Shared;

public record Tweet
{
    public bool Sensitive { get; set; }

    public string Id { get; set; }

    public string Text { get; set; }

    public string Lang { get; set; }

    [Neo4jDateTime]
    public DateOnly Date { get; set; }

    public string AuthorId { get; set; }
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
}

public record Entity
{
    public string Name { get; set; }

    public string Id { get; set; }

}
