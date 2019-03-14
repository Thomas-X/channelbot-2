using Microsoft.EntityFrameworkCore.Migrations;

namespace channelbot_2.Migrations
{
    public partial class Add_PubsubFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChannelId",
                table: "PubsubhubbubSubscriptions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subreddit",
                table: "PubsubhubbubSubscriptions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "PubsubhubbubSubscriptions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "PubsubhubbubSubscriptions");

            migrationBuilder.DropColumn(
                name: "Subreddit",
                table: "PubsubhubbubSubscriptions");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "PubsubhubbubSubscriptions");
        }
    }
}
