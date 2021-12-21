namespace TwitterTest.Models;

public record Stats
{
    public int TotalTweetCount { get; set; }
    public int FilteredTweetCount { get; set; }
}