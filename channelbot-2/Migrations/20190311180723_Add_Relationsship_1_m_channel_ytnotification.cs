using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace channelbot_2.Migrations
{
    public partial class Add_Relationsship_1_m_channel_ytnotification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    YoutubeChannelId = table.Column<string>(nullable: true),
                    Subreddit = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RedditTokens",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Token = table.Column<string>(nullable: true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    ExpirationDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedditTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YoutubeNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VideoId = table.Column<string>(nullable: true),
                    YoutubeChannelId = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    Link = table.Column<string>(nullable: true),
                    AuthorName = table.Column<string>(nullable: true),
                    AuthorLink = table.Column<string>(nullable: true),
                    PublishedDate = table.Column<string>(nullable: true),
                    UpdatedDate = table.Column<string>(nullable: true),
                    PostedToReddit = table.Column<bool>(nullable: false),
                    ChannelId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YoutubeNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YoutubeNotifications_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YoutubeNotifications_ChannelId",
                table: "YoutubeNotifications",
                column: "ChannelId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RedditTokens");

            migrationBuilder.DropTable(
                name: "YoutubeNotifications");

            migrationBuilder.DropTable(
                name: "Channels");
        }
    }
}
