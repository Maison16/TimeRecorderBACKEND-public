using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeRecorderBACKEND.Migrations
{
    /// <inheritdoc />
    public partial class workTimeAddDurationMiutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "WorkLogs",
                type: "int",
                nullable: false,
                computedColumnSql: "CASE WHEN EndTime IS NOT NULL THEN DATEDIFF(MINUTE, StartTime, EndTime) ELSE NULL END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "WorkLogs");
        }
    }
}
