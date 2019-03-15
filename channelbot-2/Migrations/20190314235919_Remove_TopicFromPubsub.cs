using Microsoft.EntityFrameworkCore.Migrations;

namespace channelbot_2.Migrations
{
    public partial class Remove_TopicFromPubsub : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Topic",
                table: "PubsubhubbubSubscriptions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "PubsubhubbubSubscriptions",
                nullable: true);
        }
    }
}
