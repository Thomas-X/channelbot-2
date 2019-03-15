﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using channelbot_2.Models;

namespace channelbot_2.Migrations
{
    [DbContext(typeof(ModelDbContext))]
    [Migration("20190314235919_Remove_TopicFromPubsub")]
    partial class Remove_TopicFromPubsub
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.2-servicing-10034")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("channelbot_2.Models.Channel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Subreddit");

                    b.Property<string>("YoutubeChannelId");

                    b.HasKey("Id");

                    b.ToTable("Channels");
                });

            modelBuilder.Entity("channelbot_2.Models.PubsubhubbubSubscription", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ChannelId");

                    b.Property<DateTime>("CreationDate");

                    b.Property<DateTime>("ExpirationDate");

                    b.Property<string>("Subreddit");

                    b.HasKey("Id");

                    b.ToTable("PubsubhubbubSubscriptions");
                });

            modelBuilder.Entity("channelbot_2.Models.RedditToken", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreationDate");

                    b.Property<DateTime>("ExpirationDate");

                    b.Property<string>("Token");

                    b.HasKey("Id");

                    b.ToTable("RedditTokens");
                });

            modelBuilder.Entity("channelbot_2.Models.YoutubeNotification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AuthorLink");

                    b.Property<string>("AuthorName");

                    b.Property<int?>("ChannelId");

                    b.Property<string>("Link");

                    b.Property<bool>("PostedToReddit");

                    b.Property<string>("PublishedDate");

                    b.Property<string>("Title");

                    b.Property<string>("UpdatedDate");

                    b.Property<string>("VideoId");

                    b.Property<string>("YoutubeChannelId");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId");

                    b.ToTable("YoutubeNotifications");
                });

            modelBuilder.Entity("channelbot_2.Models.YoutubeNotification", b =>
                {
                    b.HasOne("channelbot_2.Models.Channel", "Channel")
                        .WithMany("YoutubeNotifications")
                        .HasForeignKey("ChannelId");
                });
#pragma warning restore 612, 618
        }
    }
}
