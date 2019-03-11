using System;
using System.Collections.Generic;
using System.IO;
using channelbot_2_jsonconvert.DataStructures;
using Newtonsoft.Json;

namespace channelbot_2_jsonconvert
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO Handle invalid PMs better than just plain ignoring it
            // TODO Step 0: Investigate issue with reddit token expiring
            // TODO Step 1: Setup channelbot on server
            // TODO Step 2: Send Reddit PMs to bot with add data (limited to 20 for testing reasons)
            // TODO Step 3: Validate bot working
            // TODO HMAC for pubsubhubbub
            var bla = JsonConvert.DeserializeObject<List<ChannelJson>>(File.ReadAllText("channels.json"));
            Console.WriteLine("Hello World!");
        }
    }
}
