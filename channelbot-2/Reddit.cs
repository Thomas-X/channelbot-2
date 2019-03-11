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
    public class Reddit
    {
        public RedditAPI api;
        public static event EventHandler<Message> OnAdd;
        public static event EventHandler<Message> OnRemove;

        public Reddit(RedditAPI api)
        {
            this.api = api;
        }

        private Dictionary<string, string> GetMessageValues(string body, string[] requiredKeys)
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
            var values = GetMessageValues(message.Body, new[] {"subreddit"});
            using (var db = new ModelDbContext())
            {
                var channels = db.Channels.Where(x => x.Subreddit == values["subreddit"]).ToList();
                if (values == null || channels.Count <= 0) return;
                var sb = new StringBuilder();
                sb.AppendLine(
                    $"Below are the channels using ChannelBot2 on the specified subreddit of: {values["subreddit"]}");
                foreach (var channel in channels)
                {
                    sb.AppendLine($"ChannelID: {channel.ChannelId}");
                }

                api.Account.Messages.Compose(message.Author, $"Listing of {values["subreddit"]}", sb.ToString());
            }
        }

        public void HandleAddPm(Message message)
        {
            using (var db = new ModelDbContext())
            {
                var vals = GetMessageValues(message.Body, new[] {"channel_id", "subreddit"});
                if (vals == null) return;
                var isValid = api.Subreddit(vals["subreddit"])
                                  .Moderators
                                  // x.Name == message.Author could be wrong, this is a guess TODO test
                                  .Where(x =>
                                  {
                                      return x.Name == message.Author &&
                                             x.ModPermissions.First(y => y == "all") != null;
                                  })
                                  .ToList().Count == 1;
                if (!isValid) return;
                db.Channels.Add(new Channel()
                {
                    ChannelId = vals["channel_id"],
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
                var vals = GetMessageValues(message.Body, new[] {"channel_id", "subreddit"});
                if (vals == null) return;
                var isValid = api.Subreddit(vals["subreddit"])
                                  .Moderators
                                  .First(x => x.Name == message.Author) != null;
                if (!isValid) return;
                var channelSubscription =
                    db.Channels
                        .First(x => x.ChannelId == vals["channel_id"] && x.Subreddit == vals["subreddit"]);
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
                        break;
                    case "remove":
                        HandleRemovePm(message);
                        break;
                    case "list":
                        HandleListPm(message);
                        break;
                }

                // Mark message as "read"
                api.Account.Messages.ReadMessage(message.Name);
//                OnAdd?.Invoke(this, EventArgs.Empty);
            }
        }

        public void MonitorUnreadPMs()
        {
            api.Account.Messages.UnreadUpdated += OnUnreadPm;
            api.Account.Messages.MonitorUnread();
        }
    }
}