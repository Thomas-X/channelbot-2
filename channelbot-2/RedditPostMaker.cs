﻿using System;
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
                    var yts = db.YoutubeNotifications.Where(x => x.PostedToReddit == false);
                    foreach (var youtubeNotification in yts)
                    {
                        if (youtubeNotification == null) continue;
                        reddit.PostInSubreddit(this, youtubeNotification);
                        Thread.Sleep(1000);
                    }
                }
            }
        }
    }
}