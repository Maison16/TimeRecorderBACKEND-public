using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeRecorderBACKEND.Migrations
{
    /// <inheritdoc />
    public partial class addUpperLetterCauseItsProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "existenceStatus",
                table: "WorkLogs",
                newName: "ExistenceStatus");

            migrationBuilder.RenameColumn(
                name: "existenceStatus",
                table: "Users",
                newName: "ExistenceStatus");

            migrationBuilder.RenameColumn(
                name: "existenceStatus",
                table: "Projects",
                newName: "ExistenceStatus");

            migrationBuilder.RenameColumn(
                name: "existenceStatus",
                table: "DayOffRequests",
                newName: "ExistenceStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExistenceStatus",
                table: "WorkLogs",
                newName: "existenceStatus");

            migrationBuilder.RenameColumn(
                name: "ExistenceStatus",
                table: "Users",
                newName: "existenceStatus");

            migrationBuilder.RenameColumn(
                name: "ExistenceStatus",
                table: "Projects",
                newName: "existenceStatus");

            migrationBuilder.RenameColumn(
                name: "ExistenceStatus",
                table: "DayOffRequests",
                newName: "existenceStatus");
        }
    }
}
