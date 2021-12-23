using Shared;
using Tweetinvi.Models.V2;

namespace TwitterTest.Services;

public interface ITweetFilter
{
    bool TweetShouldBeIgnored(TweetV2 tweetV2);
}

public class TweetFilter : ITweetFilter
{

    private HashSet<string> allowedLanguages = new();
    private TweetType _ignoredTweetTypes = TweetType.None;
    private List<Func<TweetV2, bool>> _shouldBeIgnoredFunc = new();
    // private List<Func<TweetV2, Task<bool>>> _shouldBeIgnoredFuncAsync = new();

    public bool TweetShouldBeIgnored(TweetV2 tweetV2)
    {
        if (allowedLanguages.Any() && !allowedLanguages.Contains(tweetV2.Lang))
            return true;

        if (tweetV2.ReferencedTweets is not null &&
            tweetV2.ReferencedTweets.Any(x => _ignoredTweetTypes.HasFlag(x.Type())))
            return true;

        if (_shouldBeIgnoredFunc.Any(func => func.Invoke(tweetV2)))
            return true;

        // foreach (var shouldBeIgnoredAsync in _shouldBeIgnoredFuncAsync)
            // if (await shouldBeIgnoredAsync.Invoke(tweetV2))
                // return true;

        return false;
    }

    public TweetFilter AllowLanguage(params string[] languages)
    {
        foreach (string language in languages)
        {
            allowedLanguages.Add(language);
        }

        return this;
    }

    // public TweetFilter IgnoreIfAsync(Func<TweetV2, Task<bool>> filter)
    // {
        // _shouldBeIgnoredFuncAsync.Add(filter);
        // return this;
    // }

    public TweetFilter IgnoreIf(Func<TweetV2, bool> filter)
    {
        _shouldBeIgnoredFunc.Add(filter);
        return this;
    }

    public TweetFilter IgnoreTweetTypes(TweetType tweetTypes)
    {
        _ignoredTweetTypes |= tweetTypes;
        return this;
    }
}
