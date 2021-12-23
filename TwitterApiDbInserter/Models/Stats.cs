namespace TwitterTest.Models;

public record Stats
{
    public int ReceiveCount { get; set; }
    public int FilteredTweetCount { get; set; }
    public int IgnoredTweetCount { get; set; }
}