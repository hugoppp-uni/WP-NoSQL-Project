using Tweetinvi.Models.V2;

namespace TwitterTest;

public class TweetLanguageAnalyzer
{
    private int TweetSum { get; set; }

    //todo change this to be a Dict<string, int> for production instead
    private Dictionary<string, List<TweetV2>> _langTweetDict = new();

    //this is just temp for debugger
    private IEnumerable<object> TweetsPerLanguage => _langTweetDict.Select(x => new
    {
        Lang = x.Key,
        TweetCount = x.Value.Count,
        Tweets = x.Value,
        Percent = (x.Value.Count / (double)TweetSum).ToString("P")
    }).OrderByDescending(x => x.Percent.Length);

    public IEnumerable<string> TweetsPerLanguageString()
    {
        return _langTweetDict.OrderByDescending(x => x.Value.Count).Select(x => $"{x.Key}: {x.Value.Count / (double)TweetSum:P}");
    }


    public void AddTweet(TweetV2 tweetV2)
    {
        TweetSum++;

        List<TweetV2> tweetsForLanguage;
        if (!_langTweetDict.TryGetValue(tweetV2.Lang, out tweetsForLanguage!))
            tweetsForLanguage = _langTweetDict[tweetV2.Lang] = new List<TweetV2>();
        tweetsForLanguage.Add(tweetV2);
    }
}
