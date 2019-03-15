using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using channelbot_2_jsonconvert.DataStructures;
using Newtonsoft.Json;
using Reddit;

namespace channelbot_2_jsonconvert
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO store subscriptions for renewal from pubsubhubbub (important)
            // TODO Handle invalid PMs better than just plain ignoring it
            // TODO [DONE] Step 0: Investigate issue with reddit token expiring
            // TODO Step 1: Setup channelbot on server
            // TODO Step 2: Send Reddit PMs to bot with add data (limited to 20 for testing reasons)
            // TODO Step 3: Validate bot working
            // TODO HMAC for pubsubhubbub
            var channels = JsonConvert.DeserializeObject<List<ChannelJson>>(File.ReadAllText("channels.json"));
            Console.WriteLine("Hello World!");
            var reddit = new RedditAPI(accessToken: "ACCESS_TOKEN_HERE");
            var i = 0;
            foreach (var channel in channels)
            {
                i++;
                reddit.Account.Messages.Compose("Dispose_Close", "add", $"channel_id: {channel.channel_id}\nsubreddit: {channel.subreddit}");
                // Wait 5 sec to not overload bot
                Thread.Sleep(5000);
                Console.WriteLine($"processing channel index: {i}");
            }
        }
    }
}
