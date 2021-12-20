using MongoDB.Driver;
using Tweetinvi.Models.V2;

namespace TwitterTest;

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


    public static IEnumerable<TweetContextAnnotationV2> GetDistinctContextAnnotationEntities(this TweetV2 tweetV2)
    {
        if (tweetV2.ContextAnnotations is null)
        {
            return Enumerable.Empty<TweetContextAnnotationV2>();
        }

        return tweetV2.ContextAnnotations;
        // .DistinctBy(x => x.Entity.Id)
        // .Select(x => x.Entity);
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

}
