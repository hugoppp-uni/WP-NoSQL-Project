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
        return new TweetFilter()
            .AllowLanguage("de", "en", "ja")
            .IgnoreIf(tweet => (tweet.Entities.Hashtags is null || !tweet.Entities.Hashtags.Any()) &&
                               (tweet.ContextAnnotations is null || !tweet.ContextAnnotations.Any()));
    }
}
