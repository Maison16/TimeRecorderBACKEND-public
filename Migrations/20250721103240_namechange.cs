using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeRecorderBACKEND.Migrations
{
    /// <inheritdoc />
    public partial class namechange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_BotConversationReferences",
                table: "BotConversationReferences");

            migrationBuilder.RenameTable(
                name: "BotConversationReferences",
                newName: "LastWorkOnDayMassageDate");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LastWorkOnDayMassageDate",
                table: "LastWorkOnDayMassageDate",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LastWorkOnDayMassageDate",
                table: "LastWorkOnDayMassageDate");

            migrationBuilder.RenameTable(
                name: "LastWorkOnDayMassageDate",
                newName: "BotConversationReferences");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BotConversationReferences",
                table: "BotConversationReferences",
                column: "Id");
        }
    }
}
