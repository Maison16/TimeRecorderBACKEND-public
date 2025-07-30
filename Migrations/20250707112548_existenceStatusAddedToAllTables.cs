using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeRecorderBACKEND.Migrations
{
    /// <inheritdoc />
    public partial class existenceStatusAddedToAllTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "existenceStatus",
                table: "WorkLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "existenceStatus",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "existenceStatus",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "existenceStatus",
                table: "DayOffRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "existenceStatus",
                table: "WorkLogs");

            migrationBuilder.DropColumn(
                name: "existenceStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "existenceStatus",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "existenceStatus",
                table: "DayOffRequests");
        }
    }
}
