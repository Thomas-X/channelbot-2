using System;
using dotenv.net;
using Microsoft.EntityFrameworkCore;

namespace channelbot_2.Models
{
    internal class ModelDbContext : DbContext
    {
        public DbSet<YoutubeNotification> YoutubeNotifications { get; set; }
        public DbSet<RedditToken> RedditTokens { get; set; }
        public DbSet<Channel> Channels { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            DotEnv.Config();
            optionsBuilder.UseMySql(Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING") ?? "");
        }
    }
}
