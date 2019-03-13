using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Timers;
using System.Web;
using channelbot_2.DataStructures;
using channelbot_2.Models;
using Newtonsoft.Json;
using Reddit;

namespace channelbot_2
{
    public class RedditTokenManager
    {
        public static string CurrentToken;
        public Timer TokenTimer = new Timer();

        /// <summary>
        /// Refresh the reddit token
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        /// <param name="redditToken"></param>
        public void OnRefreshToken(RedditToken redditToken)
        {
            using (var db = new ModelDbContext())
            {
                // Remove old token from DB
                var tkn = GetNewToken();
                CurrentToken = tkn.Token;
                var oldToken = db.RedditTokens.Last();
                oldToken.ExpirationDate = tkn.ExpirationDate;
                oldToken.CreationDate = tkn.CreationDate;
                oldToken.Token = tkn.Token;
                db.RedditTokens.Update(
                    oldToken
                );
                // Get new token and insert into DB
                db.SaveChanges();
            }
        }

        public RedditToken GetNewToken()
        {
            using (var requestMessage =
                new HttpRequestMessage(HttpMethod.Post, "https://www.reddit.com/api/v1/access_token"))
            {
                var formData = HttpUtility.ParseQueryString(string.Empty);
                formData.Add("username", Environment.GetEnvironmentVariable("REDDIT_ACCOUNT_USERNAME"));
                formData.Add("password", Environment.GetEnvironmentVariable("REDDIT_ACCOUNT_PASSWORD"));
                formData.Add("grant_type", "password");
                var botCredentials =
                    Convert.ToBase64String(
                        Encoding.ASCII.GetBytes($"{Environment.GetEnvironmentVariable("REDDIT_BOT_ID")}:{Environment.GetEnvironmentVariable("REDDIT_BOT_SECRET")}"));
                var data = Encoding.ASCII.GetBytes(formData.ToString());
                requestMessage.Headers.Add("Authorization", "Basic " + botCredentials);
                requestMessage.Content = new ByteArrayContent(data);
                var response = Program.HttpClient.SendAsync(requestMessage)
                    .GetAwaiter()
                    .GetResult();
                var redditAccessToken =
                    JsonConvert.DeserializeObject<RedditAccessToken>(response.Content.ReadAsStringAsync()
                        .GetAwaiter().GetResult());
                var token = new RedditToken
                {
                    CreationDate = DateTime.Now,
                    ExpirationDate = DateTime.Now.AddMinutes(60),
                    Token = redditAccessToken.access_token,
                };

                // Update reddit api instance
                Reddit.Api = new RedditAPI(accessToken: token.Token);
                return token;
            }
        }

        public void Start()
        {
            using (var db = new ModelDbContext())
            {
                // Check if existing dates exist in db
                var existingTokens = db.RedditTokens
                    .Where(x => DateTime.Now < x.ExpirationDate)
                    .ToList();
                // Means we can use a token from the database
                if (existingTokens.Count > 0)
                {
                    // Set current token to existing token
                    CurrentToken = existingTokens[0].Token;
                    // Setup timer for getting new token (in case the program shut down before the token expired, would be a waste not to re-use)
                    var diff = existingTokens[0].ExpirationDate - DateTime.Now;
                    // TODO DRY
                    TokenTimer.Interval =
                        diff.TotalMilliseconds * 0.80; // use 80% of the remaining time for good measure
                    TokenTimer.Elapsed += (source, e) => OnRefreshToken(existingTokens[0]);
                    TokenTimer.Start();
                }
                else
                {
                    // Request a new token from reddit, meaning NONE exist and we should just add a new one to the DB
                    var redditToken = GetNewToken();
                    CurrentToken = redditToken.Token;
                    db.RedditTokens.Add(redditToken);
                    db.SaveChanges();

                    // Start timer 
                    TokenTimer.Interval = (redditToken.ExpirationDate - DateTime.Now).TotalMilliseconds * 0.80;
                    TokenTimer.Elapsed += (source, e) => OnRefreshToken(redditToken);
                    TokenTimer.Start();
                }
            }
        }
    }
}