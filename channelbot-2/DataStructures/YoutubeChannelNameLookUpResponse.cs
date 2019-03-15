using System;
using System.Collections.Generic;

namespace channelbot_2.DataStructures
{
    public class PageInfo
    {
        public int TotalResults { get; set; }
        public int ResultsPerPage { get; set; }
    }

    public class Id
    {
        public string Kind { get; set; }
        public string ChannelId { get; set; }
    }

    public class Default
    {
        public string Url { get; set; }
    }

    public class Medium
    {
        public string Url { get; set; }
    }

    public class High
    {
        public string Url { get; set; }
    }

    public class Thumbnails
    {
        public Default Default { get; set; }
        public Medium Medium { get; set; }
        public High High { get; set; }
    }

    public class Snippet
    {
        public DateTime PublishedAt { get; set; }
        public string ChannelId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Thumbnails Thumbnails { get; set; }
        public string ChannelTitle { get; set; }
        public string LiveBroadcastContent { get; set; }
    }

    public class Item
    {
        public string Kind { get; set; }
        public string Etag { get; set; }
        public Id Id { get; set; }
        public Snippet Snippet { get; set; }
    }

    public class YoutubChannelNameLookUpResponse
    {
        public string Kind { get; set; }
        public string Etag { get; set; }
        public string NextPageToken { get; set; }
        public string RegionCode { get; set; }
        public PageInfo PageInfo { get; set; }
        public List<Item> Items { get; set; }
    }
}