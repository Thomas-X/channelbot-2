using System.ComponentModel.DataAnnotations;

namespace channelbot_2.Models
{
    public class Channel
    {
        [Key]
        public int Id { get; set; }
        public string ChannelId { get; set; }
        public string Subreddit { get; set; }
    }
}