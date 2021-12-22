using MongoDB.Driver;
using Tweetinvi.Models.V2;

namespace Shared;

[Flags]
public enum TweetType
{
    None = 0,
    Normal = 1 << 0,
    Retweet = 1 << 1,
    Reply = 1 << 2,
    Quote = 1 << 3,
}

public static class TweetinviExtensions
{

    public static TweetType GetTweetTypes(this TweetV2 tweetV2)
    {
        if (tweetV2.ReferencedTweets is null || !tweetV2.ReferencedTweets.Any())
            return TweetType.Normal;

        TweetType tweetType = TweetType.None;
        foreach (ReferencedTweetV2 tweetV2ReferencedTweet in tweetV2.ReferencedTweets)
        {
            tweetType |= ParseTweetType(tweetV2ReferencedTweet.Type);
        }

        return tweetType;
    }

    public static TweetType ParseTweetType(string tweetType) => tweetType switch
    {
        "quoted" => TweetType.Quote,
        "retweeted" => TweetType.Retweet,
        "replied_to" => TweetType.Reply,
        _ => TweetType.None
    };

    public static IEnumerable<ReferencedTweetV2> WithType(this ReferencedTweetV2[] referencedTweets, TweetType type)
    {
        return referencedTweets.Where(x => ParseTweetType(x.Type) == type);
    }

}

public static class Extensions
{
    //https://stackoverflow.com/a/10629938/12867415
    public static IEnumerable<IEnumerable<T>> GetKCombinations<T>(this IEnumerable<T> list, int length) where T : IComparable
    {
        if (length == 1) return list.Select(t => new T[] { t });

        return GetKCombinations(list, length - 1)
            .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0),
                (t1, t2) => t1.Concat(new T[] { t2 }));
    }

    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        foreach (T obj in collection)
            action(obj);
    }

    public static IMongoDatabase GetDefaultDatabase(this IMongoClient mongoClient)
    {
        return mongoClient.GetDatabase("data");
    }

    public static IMongoCollection<TweetV2> GetTweetCollection(this IMongoClient mongoClient)
    {
        var mongoCollection = mongoClient.GetDefaultDatabase().GetCollection<TweetV2>("tweet");
        return mongoCollection;
    }

    public static DateOnly DateOnlyXDaysAgo(this DateTime date, int x) => DateOnly.FromDateTime(date).AddDays(-1 * x);

    public static string DateOnlyXDaysAgo(this DateTime date, int? x)
    {
        return x is null ? "" : DateTime.Today.DateOnlyXDaysAgo(x.Value).ToString();
    }

}
