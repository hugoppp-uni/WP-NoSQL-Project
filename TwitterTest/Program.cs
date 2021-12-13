using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters.V2;
using Tweetinvi.Streaming.V2;


var config = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly())
    .Build();

var userClient =
    new TwitterClient(new ReadOnlyConsumerCredentials(config["consumerKey"], config["consumerSecret"], config["bearerToken"]));


ISampleStreamV2 sampleStreamV2 = userClient.StreamsV2.CreateSampleStream();

int tweets = 0;
int events = 0;

sampleStreamV2.TweetReceived += (_, x) => tweets++;
sampleStreamV2.EventReceived += (_, x) => events++;

Console.WriteLine("started stream");
Stopwatch sw = Stopwatch.StartNew();
sampleStreamV2.StartAsync(new StartSampleStreamV2Parameters());

Task.Factory.StartNew(async () =>
{
    while (true)
    {
        await Task.Delay(5000);
        float elapsedSeconds = sw.ElapsedMilliseconds / 1000f;
        Console.WriteLine($"[TWEET] {tweets} ({tweets / elapsedSeconds}/s)");
        Console.WriteLine($"[EVENT] {events} ({events / elapsedSeconds}/s)");
        Console.WriteLine();
        tweets = 0;
        events = 0;
        sw.Restart();
    }
});

while (Console.ReadKey().Key != ConsoleKey.Q)
{
}

sampleStreamV2.StopStream();
