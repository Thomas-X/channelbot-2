﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using dotenv.net;
using Reddit;

namespace channelbot_2
{
    /* TODO
     * [] - Add HMAC to pubsubhubbub
     * [X] - Add reddit "listing" support
     * [X] - GET oauth.reddit.com/message/unread endpoint for getting all unread messages
     * [X] - POST oauth.reddit.com/api/read_message 
     * [] - Convert channel name to channel id in reddit PMs, atm only channel_id field is supported
     * [] - Pubsubhubbub listen to OnAdd and OnRemove events from reddit and add subscription and remove subscription
     * [] - Reddit listen to Pubsubhubbub for incoming notification (and post it to reddit)
     */
    internal class Program
    {
        public static ManualResetEvent QuitEvent = new ManualResetEvent(false);
        public static HttpClient HttpClient = new HttpClient();

        private static void Main(string[] args)
        {
            Console.WriteLine("\r\n #####                                                                    #####  \r\n#     # #    #   ##   #    # #    # ###### #      #####   ####  #####    #     # \r\n#       #    #  #  #  ##   # ##   # #      #      #    # #    #   #            # \r\n#       ###### #    # # #  # # #  # #####  #      #####  #    #   #       #####  \r\n#       #    # ###### #  # # #  # # #      #      #    # #    #   #      #       \r\n#     # #    # #    # #   ## #   ## #      #      #    # #    #   #      #       \r\n #####  #    # #    # #    # #    # ###### ###### #####   ####    #      ####### \r\n");
            DotEnv.Config();
            Console.CancelKeyPress += (sender, eArgs) => {
                QuitEvent.Set();
                eArgs.Cancel = true;
            };

            Console.WriteLine(Environment.GetEnvironmentVariable("REDDIT_BOT_ID"));
            Console.WriteLine(Environment.GetEnvironmentVariable("REDDIT_BOT_SECRET"));
            Console.WriteLine(Environment.GetEnvironmentVariable("REDDIT_ACCOUNT_USERNAME"));
            Console.WriteLine(Environment.GetEnvironmentVariable("REDDIT_ACCOUNT_PASSWORD"));
            Console.WriteLine(Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING"));

            // Setup RedditToken for use in polling etc.
            var redditTokenManager = new RedditTokenManager();
            redditTokenManager.Start();

            // Setup Reddit Client
            var redditAPI = new RedditAPI(accessToken:RedditTokenManager.CurrentToken);
            using (var reddit = new Reddit(redditAPI))
            {
                reddit.MonitorUnreadPMs();
//
//                //Start polling
//                var pollManager = new PollManager();
//                pollManager.Start();
//
//                // Start pubsubhubbub, call dispose on it to remove listeners from event
//                using (var hubbub = new PubSubHubBub())
//                {
//                    hubbub.Start();
//                    QuitEvent.WaitOne();
//                }
            } 
        }
    }
}
