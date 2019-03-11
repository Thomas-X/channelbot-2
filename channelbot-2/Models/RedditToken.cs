using System;

namespace channelbot_2.Models
{
    public class RedditToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}