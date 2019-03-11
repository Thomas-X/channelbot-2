using System;
using System.Linq;
using System.Timers;
using channelbot_2.Interfaces;
using channelbot_2.Models;

namespace channelbot_2
{
    public class RedditPoller : IPoller
    {
        // Poll PMs every 30s
        public int PollInterval { get; set; } = 30000;
        

        // On setup, get auth etc.
        public void OnSetup()
        {
            Console.WriteLine("On setup!");
        }

//        public void On

        public void OnPoll(object source, ElapsedEventArgs e)
        {
        }
        
    }
}