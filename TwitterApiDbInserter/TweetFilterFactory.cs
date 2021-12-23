using Microsoft.Extensions.DependencyInjection;
using Neo4jClient;
using Shared;
using Tweetinvi.Models.V2;
using TwitterTest.Services;

namespace TwitterTest;

public interface IServiceFactory<T>
{
    public T Create(IServiceProvider provider);
}

public class TweetFilterFactory : IServiceFactory<ITweetFilter>
{
    public ITweetFilter Create(IServiceProvider provider)
    {
        var neo4J = provider.GetRequiredService<Neo4JDataAccess>();
        return new TweetFilter()
            .AllowLanguage("de", "en", "ja")
            .IgnoreIf(tweet => (tweet.Entities.Hashtags is null || !tweet.Entities.Hashtags.Any()) &&
                               (tweet.ContextAnnotations is null || !tweet.ContextAnnotations.Any()))
            .IgnoreIfAsync(async (tweet) =>
            {
                ReferencedTweetV2? referencedTweetRetweet =
                    tweet.ReferencedTweets?.FirstOrDefault(x => x.Type() == TweetType.Retweet);

                //dont filter out non-retweets
                if (referencedTweetRetweet is null) return false;

                //retweets are ok, as long the original tweet does not exist
                bool originalTweetAlreadyExists = await neo4J.TweetWithIdExists(referencedTweetRetweet.Id);
                return originalTweetAlreadyExists;
            });
    }
}
