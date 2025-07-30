using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeRecorderBACKEND.Migrations
{
    /// <inheritdoc />
    public partial class newFieldsInSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SyncUsersDays",
                table: "Settings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "SyncUsersFrequency",
                table: "Settings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SyncUsersHour",
                table: "Settings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SyncUsersMonthDay",
                table: "Settings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SyncUsersDays",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SyncUsersFrequency",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SyncUsersHour",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SyncUsersMonthDay",
                table: "Settings");
        }
    }
}
