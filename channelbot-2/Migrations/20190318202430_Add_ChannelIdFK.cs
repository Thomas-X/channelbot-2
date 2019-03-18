using Microsoft.EntityFrameworkCore.Migrations;

namespace channelbot_2.Migrations
{
    public partial class Add_ChannelIdFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_YoutubeNotifications_Channels_ChannelId",
                table: "YoutubeNotifications");

            migrationBuilder.AlterColumn<int>(
                name: "ChannelId",
                table: "YoutubeNotifications",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_YoutubeNotifications_Channels_ChannelId",
                table: "YoutubeNotifications",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_YoutubeNotifications_Channels_ChannelId",
                table: "YoutubeNotifications");

            migrationBuilder.AlterColumn<int>(
                name: "ChannelId",
                table: "YoutubeNotifications",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddForeignKey(
                name: "FK_YoutubeNotifications_Channels_ChannelId",
                table: "YoutubeNotifications",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
