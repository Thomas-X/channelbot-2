using System;
using System.Timers;
using channelbot_2.Interfaces;
using channelbot_2.Models;
using Reddit.Things;

namespace channelbot_2
{
    public class PubsubhubbubSubscriptionRefresher : IPoller
    {
        // poll every 24h
        public int PollInterval { get; set; } = 86400000;

        public void OnSetup()
        {
        }

        /// <summary>
        /// Polls every 24h if any pubsub should be refreshed
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnPoll(object source, ElapsedEventArgs e)
        {
            // Store subscription in db, set timer for when we need to refresh
            using (var db = new ModelDbContext())
            {
                foreach (var pubsubhubbubSubscription in db.PubsubhubbubSubscriptions)
                {
                    var diff = DateTime.Now - pubsubhubbubSubscription.ExpirationDate;
                    // If there are less than 2 days left on the subscription, refresh it
                    if (diff.TotalDays <= 2)
                    {
                        var pubsub = new PubSubHubBub();
                        pubsub.Subscribe(new {}, new Message()
                        {
                            Body = $"channel_id: {pubsubhubbubSubscription.ChannelId}\r\nsubreddit: {pubsubhubbubSubscription.Subreddit}"
                        });
                    }
                }

                //                var sub = db.PubsubhubbubSubscriptions.First(x => x.Topic == query["hub.topic"]);
                //                if (sub == null)
                //                {
                //                    db.PubsubhubbubSubscriptions.Add(
                //                        new PubsubhubbubSubscription()
                //                        {
                //                            CreationDate = DateTime.Now,
                //                            ExpirationDate = DateTime.Now.AddDays(4),
                //                            Topic = query["hub.topic"]
                //                        }
                //                    );
                //                }
            }
        }
    }
}