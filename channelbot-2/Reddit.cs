using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using channelbot_2.DataStructures;
using channelbot_2.Models;
using Newtonsoft.Json;
using Reddit;
using Reddit.Controllers.EventArgs;
using Reddit.Things;

namespace channelbot_2
{
    public class Reddit : IDisposable
    {
        private static RedditAPI _api;
        public static RedditAPI Api
        {
            get => _api;
            set
            {
                lock (ApiLock)
                {
                    _api = value;
                }
            }
        }
        public static readonly object ApiLock = new object();
        public static event EventHandler<Message> OnAdd;
        public static event EventHandler<Message> OnRemove;
        public static string[] RequiredKeysListPm = {"subreddit"};
        public static string[] RequiredKeysAddPm = {"channel_id", "subreddit"};
        public static string[] RequiredKeysRemovePm = {"channel_id", "subreddit"};

        public Reddit(RedditAPI api)
        {
            PubSubHubBub.OnNotificationReceived += PostInSubreddit;
            Api = api;
        }

        public void PostInSubreddit(object source, YoutubeNotification yt)
        {
            if (source == null)
            {
                Console.WriteLine("source is null");
            }
            else if (yt == null)
            {
                Console.WriteLine("yt is null when it really shouldn't be..");
            }

            try
            {
                if (yt == null) return;
                Api.Subreddit(yt.Channel.Subreddit)
                    .LinkPost($"{yt.Title} by {yt.AuthorName}", yt.Link)
                    .Submit(resubmit: true)
                    .Reply(
                        $"Video published at {yt.PublishedDate} to {yt.Channel.YoutubeNotifications.Count} subreddit(s).");
                using (var db = new ModelDbContext())
                {
                    yt.PostedToReddit = true;
                    db.YoutubeNotifications.Update(yt);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Gets values from a set of body strings
        /// </summary>
        /// <param name="body"></param>
        /// <param name="requiredKeys"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetMessageValues(string body, string[] requiredKeys)
        {
            try
            {
                var dict = new Dictionary<string, string>();
                var vals = body.Split("\n");
                var skipIter = false;
                foreach (var val in vals)
                {
                    var splitted = val.Split(":");
                    KeyValuePair<string, string> keyValue = new KeyValuePair<string, string>(splitted[0].ToLower(), splitted[1]);

                    // so if we converted the channel field to channel_id.. realllllyyyyy uglyy
                    if (skipIter && dict.ContainsKey("channel_id") && keyValue.Key == "channel")
                    {
                        continue;
                    }

                    if (keyValue.Key == "channel" && keyValue.Value.Length > 0)
                    {
                        Console.WriteLine("Converting channel to channel_id..");
                        var res = JsonConvert.DeserializeObject<YoutubChannelNameLookUpResponse>(
                            Program.HttpClient.GetStringAsync(
                                    $"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&type=channel&q={HttpUtility.UrlEncode(keyValue.Value.Trim())}&key={Environment.GetEnvironmentVariable("YOUTUBE_API_KEY")}")
                                .GetAwaiter().GetResult());
                        if (res.Items.Count <= 0) continue;
                        dict["channel_id"] = res.Items[0].Id.ChannelId;
                        skipIter = true;
                    }
                    else if (requiredKeys.Any(keyValue.Key.Contains) && keyValue.Value.Length > 0)
                    {
                        dict[requiredKeys.First(x => x == keyValue.Key).Trim()] = keyValue.Value.Trim();
                    }
                    // If anything of the request is invalid, ignore it and read as marked
                    else
                    {
                        Console.WriteLine("Gotten an invalid msg");
                        return null;
                    }
                }


                return dict;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public void HandleListPm(Message message)
        {
            var values = GetMessageValues(message.Body, RequiredKeysListPm);
            if (values == null)
            {
                PubSubHubBub.ChannelNameNotSupportedMessage(message.Author);
                return;
            }

            using (var db = new ModelDbContext())
            {
                var channels = db.Channels.Where(x => x.Subreddit == values["subreddit"]).ToList();
                if (values == null || channels.Count <= 0) return;
                var sb = new StringBuilder();
                sb.AppendLine(
                    $"Below are the channels using ChannelBot2 on the specified subreddit of: {values["subreddit"]}\r\n");
                foreach (var channel in channels)
                {
                    sb.AppendLine($"ChannelID: {channel.YoutubeChannelId}\r\n");
                }

                Api.Account.Messages.Compose(message.Author, $"Listing of {values["subreddit"]}\r\n", sb.ToString());
            }
        }

        public void HandleAddPm(Message message)
        {
            using (var db = new ModelDbContext())
            {
                var vals = GetMessageValues(message.Body, RequiredKeysAddPm);
                if (vals == null)
                {
                    PubSubHubBub.ChannelNameNotSupportedMessage(message.Author);
                    return;
                }

                var isValid = Api.Subreddit(vals["subreddit"])
                                  .Moderators
                                  .Where(x => x.Name == message.Author &&
                                              x.ModPermissions.FirstOrDefault(y => y == "all") != null)
                                  .ToList().Count == 1;
                if (!isValid) return;
                var exists = db.Channels.FirstOrDefault(x =>
                                 x.YoutubeChannelId == vals["channel_id"] && x.Subreddit == vals["subreddit"]) != null;
                if (exists) return;
                db.Channels.Add(new Channel()
                {
                    YoutubeChannelId = vals["channel_id"],
                    Subreddit = vals["subreddit"]
                });
                db.SaveChanges();
            }

            // Alert pubsubhubbub to unsubscribe
            OnAdd?.Invoke(this, message);
        }

        public void HandleRemovePm(Message message)
        {
            using (var db = new ModelDbContext())
            {
                var vals = GetMessageValues(message.Body, RequiredKeysRemovePm);
                if (vals == null)
                {
                    PubSubHubBub.ChannelNameNotSupportedMessage(message.Author);
                    return;
                }

                var isValid = Api.Subreddit(vals["subreddit"])
                                  .Moderators
                                  .FirstOrDefault(x => x.Name == message.Author) != null;
                if (!isValid) return;
                var channelSubscription =
                    db.Channels
                        .FirstOrDefault(x =>
                            x.YoutubeChannelId == vals["channel_id"] && x.Subreddit == vals["subreddit"]);
                if (channelSubscription == null) return;
                db.Channels.Remove(
                    channelSubscription
                );
                db.SaveChanges();
            }

            // Alert pubsubhubbub to unsubscribe
            OnRemove?.Invoke(this, message);
        }

        public void OnUnreadPm(object sender, MessagesUpdateEventArgs e)
        {
            foreach (var message in e.NewMessages)
            {
                if (message.Author != null)
                {
                    Console.WriteLine($"incoming message from {message.Author}!");
                    switch (message.Subject)
                    {
                        case "add":
                            HandleAddPm(message);
//                        Api.Account.Messages.Compose(message.Author, "success", "successfully added your channel to the bot");
                            break;
                        case "remove":
                            HandleRemovePm(message);
//                        Api.Account.Messages.Compose(message.Author, "success", "successfully removed your channel from the bot");
                            break;
                        case "list":
                            HandleListPm(message);
                            break;
                        default:
                            Api.Account.Messages.Compose(message.Author, "error", "invalid PM title");
                            break;
                    }
                }

                // Mark message as "read"
                Api.Account.Messages.ReadMessage(message.Name);
                // wait 5 sec between reading messages because reddit API likes throttling like there is no tomorrow.
                Thread.Sleep(5000);
            }
        }

        public void MonitorUnreadPMs()
        {
            Api.Account.Messages.UnreadUpdated += OnUnreadPm;
            // monitor pms every 120s
            Api.Account.Messages.MonitorUnread(120000);
        }

        public void Dispose()
        {
            Api.Account.Messages.UnreadUpdated -= OnUnreadPm;
            PubSubHubBub.OnNotificationReceived -= PostInSubreddit;
        }
    }
}