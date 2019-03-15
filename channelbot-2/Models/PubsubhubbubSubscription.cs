using System;
using System.ComponentModel.DataAnnotations;

namespace channelbot_2.Models
{
    public class PubsubhubbubSubscription
    {
        [Key] public int Id { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string ChannelId { get; set; }
        public string Subreddit { get; set; }
    }
}