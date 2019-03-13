using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using channelbot_2.Models;
using Reddit.Things;

namespace channelbot_2
{
    /// <inheritdoc />
    /// <summary>
    /// Handles all pubsubhubbub connection and events
    /// from https://pubsubhubbub.appspot.com
    /// for more info in pubsubhubbub check out:
    ///     -https://en.wikipedia.org/wiki/WebSub
    ///     -https://developers.google.com/youtube/v3/guides/push_notifications
    /// </summary>
    public class PubSubHubBub : IDisposable
    {
     
        public static event EventHandler<YoutubeNotification> OnNotificationReceived;
        

        // 1000000 byte = 1mb 
        private int _receivingByteSize = 1000000;
        private const string subscribe = "subscribe";
        private const string unsubscribe = "unsubscribe";
        private const string get = "GET";
        private const string post = "POST";

        // TODO see https://i.imgur.com/A58fJ8M.png
        // for these methods call info 

        public PubSubHubBub()
        {
            Reddit.OnAdd += Subscribe;
            Reddit.OnRemove += Unsubscribe;
        }

        public void Dispose()
        {
            Reddit.OnAdd -= Subscribe;
            Reddit.OnRemove -= Unsubscribe;
        }

        /// <summary>
        /// Method that handles making the call to pubsubhubbub that we wish to subscribe to a topic
        /// </summary>
        public void Subscribe(object sender, Message message)
        {
            var vals = Reddit.GetMessageValues(message.Body, Reddit.RequiredKeysAddPm);
            if (vals == null) return;
            var dict = HttpUtility.ParseQueryString(string.Empty);
            dict.Add("hub.mode", "subscribe");
            dict.Add("hub.topic", $"https://www.youtube.com/xml/feeds/videos.xml?channel_id={vals["channel_id"]}");
            dict.Add("hub.callback", $"{Environment.GetEnvironmentVariable("REACHABLE_ADDRESS")}");
            Program.HttpClient.PostAsync("https://pubsubhubbub.appspot.com/subscribe", new ByteArrayContent(
                        Encoding.ASCII.GetBytes(dict.ToString())
                    )
                )
                .GetAwaiter()
                .GetResult();
            Console.WriteLine($"subscribed to {vals["channel_id"]} in /r/{vals["subreddit"]}");
        }

        /// <summary>
        /// Method that handles making the call to pubsubhubbub that we wish to unsubscribe from a topic
        /// </summary>
        public void Unsubscribe(object sender, Message message)
        {
            var vals = Reddit.GetMessageValues(message.Body, Reddit.RequiredKeysRemovePm);
            if (vals == null) return;
            var dict = HttpUtility.ParseQueryString(string.Empty);
            dict.Add("hub.mode", "unsubscribe");
            dict.Add("hub.topic", $"https://www.youtube.com/xml/feeds/videos.xml?channel_id={vals["channel_id"]}");
            dict.Add("hub.callback", Environment.GetEnvironmentVariable("REACHABLE_ADDRESS"));

            Program.HttpClient.PostAsync("https://pubsubhubbub.appspot.com/subscribe", new ByteArrayContent(
                        Encoding.ASCII.GetBytes(dict.ToString())
                    )
                )
                .GetAwaiter()
                .GetResult();
            Console.WriteLine($"unsubscribed from {vals["channel_id}"]} in /r/{vals["subreddit"]}");
        }

        /// <summary>
        /// Handle an incoming notification from pubsubhubbub
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="body"></param>
        private void OnNotification(string[] headers, string body)
        {
            // Check if correct content type, if yes, _assume_ it's an update
            // TODO change this to use the HMAC integrity, since this is vulnerable to attacks
            // TODO use HMAC secret for integrity check

            var isValidContentType = false;
            foreach (var header in headers)
            {
                var _h = header.Split(":");
                if (_h[0] == "Content-Type" && _h[1].Trim() == "application/atom+xml")
                {
                    isValidContentType = true;
                }
            }

            if (!isValidContentType)
            {
                return;
            }

            body = body.Replace("\r\n\r\n", "");
            var xml = XDocument.Parse(body);
            var atomNs = xml.Root.Name.Namespace;
            var ytNs = "{http://www.youtube.com/xml/schemas/2015}";
            // Free memory of db inst after context
            using (var db = new ModelDbContext())
            {
                // Read all entries, adding each into the DB
                foreach (var descendant in xml.Descendants(atomNs + "entry"))
                {
                    Func<IEnumerable<XElement>, string> xmlDescandantValueGetter = el => el.ToArray()[0].Value;
                    // Check if notification of that video already exists (this means it was updated instead of created and
                    // we should ignore it).
                    var videoId = descendant.Descendants(ytNs + "videoId").ToArray()[0].Value;
                    var exists = db.YoutubeNotifications.Where(x => x.VideoId == videoId).ToList().Count > 0;
                    // Check if notification was made within the hour. (if it's just been uploaded the published time is within the hour, unless pubsubhubbub is really slow)
                    var dateToCheck =
                        DateTime.Parse(xmlDescandantValueGetter(descendant.Descendants(atomNs + "published")));
                    var withinTheHour = dateToCheck > DateTime.Now.Subtract(new TimeSpan(1, 0, 0)) &&
                                        dateToCheck < DateTime.Now.AddHours(1);

                    if (exists) return;
                    if (!withinTheHour) return;
                    // add it to DB
                    var author = descendant.Descendants(atomNs + "author");
                    var xAuthor = author.ToList();
                    // TODO enable within the hour check
                    // Get all channels/subreddit combos with the channel
                    // Since there could be many subreddits for one channel
                    foreach (var channel in db.Channels)
                    {
                        if (channel.YoutubeChannelId !=
                            xmlDescandantValueGetter(descendant.Descendants(ytNs + "channelId"))) return;

                        var yt = new YoutubeNotification
                        {
                            AuthorLink = xmlDescandantValueGetter(xAuthor.Descendants(atomNs + "uri")),
                            AuthorName = xmlDescandantValueGetter(xAuthor.Descendants(atomNs + "name")),
                            YoutubeChannelId =
                                xmlDescandantValueGetter(descendant.Descendants(ytNs + "channelId")),
                            VideoId = videoId,
                            Link = descendant.Descendants(atomNs + "link").Attributes("href").ToArray()[0]
                                .Value,
                            PublishedDate =
                                xmlDescandantValueGetter(descendant.Descendants(atomNs + "published")),
                            Title = xmlDescandantValueGetter(descendant.Descendants(atomNs + "title")),
                            UpdatedDate = xmlDescandantValueGetter(descendant.Descendants(atomNs + "updated")),
                            PostedToReddit = false,
                            Channel = channel
                        };

                        db.YoutubeNotifications.Add(
                           yt
                        );
                        OnNotificationReceived?.Invoke(this, yt);
                    }
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Accepts an incoming connection asynchronously and handles logic in a separate thread to avoid blocking the next call
        /// </summary>
        /// <param name="result"></param>
        private void OnConnected(IAsyncResult result)
        {
            var listener = (TcpListener) result.AsyncState;
            TcpClient client;
            try
            {
                // Get the client 
                client = listener.EndAcceptTcpClient(result);
            }
            // If server socket is closed, catch error and return
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return;
            }

            listener.BeginAcceptTcpClient(OnConnected, listener);
            // Handle request/response logic in new thread 
            Task.Factory.StartNew(() => { HandleRequest(client); });
        }

        /// <summary>
        /// Writes verification body to the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="content"></param>
        private void HandleVerification(NetworkStream stream, string content)
        {
            stream.Write(Encoding.UTF8.GetBytes(content));
        }

        /// <summary>
        /// When the request is given from pubsubhubbub to unsubscribe
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="query"></param>
        private void OnSubscribeVerification(NetworkStream stream, NameValueCollection query)
        {
            // TODO add challenge to db?
            var str =
                $"HTTP/1.1 200 OK\r\nAccept-Ranges:bytes\r\nContent-Length:{query["hub.challenge"].Length}\r\n\r\n{query["hub.challenge"]}";
            HandleVerification(stream, str);
        }

        /// <summary>
        /// Handles an incoming request if it's an unsubscribe from an subscription 
        /// </summary>
        private void OnUnsubscribeVerfication(NetworkStream stream, NameValueCollection query)
        {
            // TODO remove values from subscription from DB
            var str =
                $"HTTP/1.1 200 OK\r\nAccept-Ranges:bytes\r\nContent-Length:{query["hub.challenge"].Length}\r\n\r\n{query["hub.challenge"]}";
            HandleVerification(stream, str);
        }

        /// <summary>
        /// Handles incoming request and determines whether its a:
        ///     -Subscribe verification
        ///     -Unsubscribe verification
        ///     -Notification from an updated/published video
        /// </summary>
        /// <param name="client"></param>
        private void HandleRequest(TcpClient client)
        {
            var stream = client.GetStream();
            int x;
            var buffer = new byte[_receivingByteSize];
            while ((x = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                try
                {
                    var data = Encoding.UTF8.GetString(buffer, 0, x);
                    var head = data.Substring(0, data.IndexOf("\r\n\r\n", StringComparison.Ordinal));
                    var body = data.Substring(data.IndexOf("\r\n\r\n", StringComparison.Ordinal));
                    var headers = head.Split("\r\n");
                    var queryString = headers[0].Split(" ")[1];
                    var type = headers[0].Split(" ")[0];
                    var query = HttpUtility.ParseQueryString(queryString);
                    Console.WriteLine($"Received data from pubsubhubbub, url is: \r\n{headers[0]}");
                    // On subscribe verification
                    if (query["hub.challenge"] != null
                        && query["hub.mode"] == subscribe
                        && type.ToUpper() == get)
                    {
                        OnSubscribeVerification(stream, query);
                    }
                    // On unsubscribe verification
                    else if (query["hub.challenge"] != null
                             && query["hub.mode"] == unsubscribe
                             && type.ToUpper() == get)
                    {
                        OnUnsubscribeVerfication(stream, query);
                    }
                    // Assume the request is a notification 
                    else if (type == post)
                    {
                        OnNotification(headers, body);
                    }

                    if (!stream.DataAvailable || !stream.CanRead || !stream.CanWrite)
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            stream.Close();
            stream.Dispose();
            client.Close();
            client.Dispose();
        }

        /// <summary>
        /// On start of the pubsubhubbub class
        /// </summary>
        public void Start()
        {
            // Start listening on the TCP socket
            var port = Environment.GetEnvironmentVariable("PORT");
            if (!int.TryParse(port, out var parsedPort)) return;
            var localAddr = IPAddress.Any;
            var server = new TcpListener(localAddr, parsedPort);

            // Accept incoming requests
            server.Start();

            server.BeginAcceptTcpClient(OnConnected, server);
        }
    }
}