using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniiaAnonim.TGBot.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddAuthorIndexesAndBannedUsersTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BannedUsers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TelegramIdHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BannedUsers", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_StoryAuthor_AuthorIdHash_Status",
            table: "StoryAuthor",
            columns: ["AuthorIdHash", "Status"]);

        migrationBuilder.CreateIndex(
            name: "IX_StoryAuthor_ChannelMessageId",
            table: "StoryAuthor",
            column: "ChannelMessageId");

        migrationBuilder.CreateIndex(
            name: "IX_BannedUsers_TelegramIdHash",
            table: "BannedUsers",
            column: "TelegramIdHash",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BannedUsers");

        migrationBuilder.DropIndex(
            name: "IX_StoryAuthor_AuthorIdHash_Status",
            table: "StoryAuthor");

        migrationBuilder.DropIndex(
            name: "IX_StoryAuthor_ChannelMessageId",
            table: "StoryAuthor");
    }
}
