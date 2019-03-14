using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using channelbot_2.Models;
using Reddit;
using Reddit.Controllers.EventArgs;
using Reddit.Things;

namespace channelbot_2
{
    public class Reddit : IDisposable
    {
        public static RedditAPI Api;
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
            try
            {
                Api.Subreddit(yt.Channel.Subreddit)
                    .LinkPost($"{yt.Title} by {yt.AuthorName}", yt.Link)
                    .Submit(resubmit: true)
                    .Reply(
                        $"Video published at {yt.PublishedDate} to {yt.Channel.YoutubeNotifications.Count} subreddits.");
                using (var db = new ModelDbContext())
                {
                    yt.PostedToReddit = true;
                    db.YoutubeNotifications.Update(yt);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static Dictionary<string, string> GetMessageValues(string body, string[] requiredKeys)
        {
            var dict = new Dictionary<string, string>();
            var vals = body.Split("\n");
            foreach (var val in vals)
            {
                var splitted = val.Split(":");
                KeyValuePair<string, string> keyValue = new KeyValuePair<string, string>(splitted[0], splitted[1]);
                if (requiredKeys.Any(keyValue.Key.Contains) && keyValue.Value.Length > 0)
                {
                    dict[requiredKeys.First(x => x == keyValue.Key).Trim()] = keyValue.Value.Trim();
                }
                // If anything of the request is invalid, ignore it and read as marked
                else
                {
                    return null;
                }
            }

            return dict;
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
                Console.WriteLine($"incoming message from {message.Author}!");
                switch (message.Subject)
                {
                    case "add":
                        HandleAddPm(message);
                        Api.Account.Messages.Compose(message.Author, "success", "successfully added your channel to the bot");
                        break;
                    case "remove":
                        HandleRemovePm(message);
                        Api.Account.Messages.Compose(message.Author, "success", "successfully removed your channel from the bot");
                        break;
                    case "list":
                        HandleListPm(message);
                        break;
                    default:
                        Api.Account.Messages.Compose(message.Author, "error", "invalid PM title");
                        break;
                }

                // Mark message as "read"
                Api.Account.Messages.ReadMessage(message.Name);
            }
        }

        public void MonitorUnreadPMs()
        {
            Api.Account.Messages.UnreadUpdated += OnUnreadPm;
            // monitor pms every 30s
            Api.Account.Messages.MonitorUnread(30000);
        }

        public void Dispose()
        {
            Api.Account.Messages.UnreadUpdated -= OnUnreadPm;
            PubSubHubBub.OnNotificationReceived -= PostInSubreddit;
        }
    }
}