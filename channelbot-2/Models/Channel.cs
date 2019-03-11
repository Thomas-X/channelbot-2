using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace channelbot_2.Models
{
    public class Channel
    {
        [Key]
        public int Id { get; set; }
        public string YoutubeChannelId { get; set; }
        public string Subreddit { get; set; }
        public List<YoutubeNotification> YoutubeNotifications { get; set; }
    }
}