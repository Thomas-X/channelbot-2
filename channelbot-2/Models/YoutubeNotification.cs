﻿namespace channelbot_2.Models
{
    public class YoutubeNotification
    {
        public int Id { get; set; }
        public string VideoId { get; set; }
        public string YoutubeChannelId { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string AuthorName { get; set; }
        public string AuthorLink { get; set; }
        public string PublishedDate { get; set; }
        public string UpdatedDate { get; set; }
        public bool PostedToReddit { get; set; }
        public int ChannelId { get; set; }
        public Channel Channel { get; set; }
    }
}