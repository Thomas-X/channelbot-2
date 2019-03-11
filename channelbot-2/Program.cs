using System;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
        public static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        private static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, eArgs) => {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };
            
            // Setup RedditToken for use in polling etc.
            var redditTokenManager = new RedditTokenManager();
            redditTokenManager.Start();

            // Setup Reddit Client
            var redditAPI = new RedditAPI(accessToken:RedditTokenManager.CurrentToken);
            var reddit = new Reddit(redditAPI);
            reddit.MonitorUnreadPMs();
            
            // TODO remove this
            //Start polling
//            var pollManager = new PollManager();
//            pollManager.Start();

            // Start pubsubhubbub
            var hubbub = new PubSubHubBub(); 
            hubbub.Start();

            // TODO Unmonitor reddit
            _quitEvent.WaitOne();
        }
    }
}
