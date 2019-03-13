using System.Collections.Generic;

namespace channelbot_2_jsonconvert.DataStructures
{
    public class ChannelJson
    {
        public string channel { get; set; }
        public string channel_id { get; set; }
        public string subreddit { get; set; }
        public string user { get; set; }
        public int register_date { get; set; }
        public string upload_playlist { get; set; }
        public List<string> last_videos { get; set; }
        public int last_check { get; set; }
    }
}