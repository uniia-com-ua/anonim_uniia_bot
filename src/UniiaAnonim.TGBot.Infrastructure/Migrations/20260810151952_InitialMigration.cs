using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniiaAnonim.TGBot.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Channel",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<byte>(type: "smallint", nullable: false),
                ChannelId = table.Column<long>(type: "bigint", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Channel", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "StoryAuthor",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AuthorId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                AuthorIdHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                ChannelMessageId = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StoryAuthor", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "StoryFileEntity",
            columns: table => new
            {
                StoryId = table.Column<Guid>(type: "uuid", nullable: false),
                FileId = table.Column<string>(type: "text", nullable: false),
                Type = table.Column<byte>(type: "smallint", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StoryFileEntity", x => new { x.StoryId, x.FileId });
                table.ForeignKey(
                    name: "FK_StoryFileEntity_StoryAuthor_StoryId",
                    column: x => x.StoryId,
                    principalTable: "StoryAuthor",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Channel_ChannelId",
            table: "Channel",
            column: "ChannelId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Channel_Type",
            table: "Channel",
            column: "Type");

        migrationBuilder.CreateIndex(
            name: "IX_StoryAuthor_AuthorIdHash",
            table: "StoryAuthor",
            column: "AuthorIdHash");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Channel");

        migrationBuilder.DropTable(
            name: "StoryFileEntity");

        migrationBuilder.DropTable(
            name: "StoryAuthor");
    }
}
