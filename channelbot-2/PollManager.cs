﻿using System;
using System.Collections.Generic;
using System.Timers;
using channelbot_2.Interfaces;

namespace channelbot_2
{
    public class PollManager
    {
        public List<Type> pollers = new List<Type>()
        {
            typeof(RedditPostMaker),
            typeof(PubsubhubbubSubscriptionRefresher)
        };

        public List<Timer> timers = new List<Timer>();

        public void Start()
        {
            // Init all timers
            foreach (var poller in pollers)
            {
                var pollerInstance = (IPoller) Activator.CreateInstance(poller);
                var timer = new Timer {Interval = pollerInstance.PollInterval};
                timer.Elapsed += pollerInstance.OnPoll;
                // Call poll once on bootup
                pollerInstance.OnPoll(new {}, null);
                pollerInstance.OnSetup();
                timers.Add(timer);
            }
            // Start all timers
            foreach (var timer in timers)
            {
                timer.Start();
            }
        }
    }
}