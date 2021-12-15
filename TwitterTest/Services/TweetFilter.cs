using Tweetinvi.Models.V2;

namespace TwitterTest.Services;

public class TweetFilter
{

    private HashSet<string> allowedLanguages = new();

    public TweetFilter AllowLanguage(params string[] languages)
    {
        foreach (string language in languages)
        {
            allowedLanguages.Add(language);
        }

        return this;
    }

    public bool TweetShouldBeIgnored(TweetV2 tweetV2)
    {
        if (allowedLanguages.Contains(tweetV2.Lang))
            return false;

        return true;
    }
}
