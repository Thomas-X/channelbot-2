using System;
using System.Linq;
using System.Threading;
using System.Timers;
using channelbot_2.Interfaces;
using channelbot_2.Models;

namespace channelbot_2
{
    public class RedditPostMaker : IPoller
    {
        // Post messages every 2m, check for it (incase the reddit request failed from the events)
        public int PollInterval { get; set; } = 120000;
        
        /// <summary>
        /// On setup
        /// </summary>
        public void OnSetup()
        {
            Console.WriteLine("On setup!");
        }

        /// <summary>
        /// On poll, currently every 2m
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnPoll(object source, ElapsedEventArgs e)
        {
            using (var reddit = new Reddit(Reddit.Api))
            {
                using (var db = new ModelDbContext())
                {
                    foreach (var yt in db.YoutubeNotifications.Where(x => x.PostedToReddit == false))
                    {
                        reddit.PostInSubreddit(new {}, yt);
                        // Wait one sec between requests (should be internally managed by Reddit.NET)
                        Thread.Sleep(1000);
                        Console.WriteLine("Backup reddit posting!");
                    }
                }
            }
        }
        
    }
}