using System;
using System.Linq;
using System.Threading;
using System.Timers;
using channelbot_2.Interfaces;
using channelbot_2.Models;
using Microsoft.EntityFrameworkCore;

namespace channelbot_2
{
    public class RedditPostMaker : IPoller
    {
        // Post messages every 2m, check for it (incase the reddit request failed from the events)
        public int PollInterval { get; set; } = 20000; // 120000

        /// <summary>
        /// On setup
        /// </summary>
        public void OnSetup()
        {
        }

        /// <summary>
        /// On poll, currently every 2m
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnPoll(object source, ElapsedEventArgs e)
        {
            using (var db = new ModelDbContext())
            {
                var yts = db.YoutubeNotifications.Where(x => x.PostedToReddit == false)
                    .ToList();
                foreach (var youtubeNotification in yts)
                {
                    if (youtubeNotification == null) continue;
                    youtubeNotification.Channel =
                        db.Channels.FirstOrDefault(y => y.Id == youtubeNotification.ChannelId);
                    Console.WriteLine("On poll");
                    Program.reddit.PostInSubreddit(new {}, youtubeNotification);
                    Thread.Sleep(1000);
                }
            }
        }
    }
}