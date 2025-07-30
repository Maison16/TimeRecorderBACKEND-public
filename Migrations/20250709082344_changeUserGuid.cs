using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeRecorderBACKEND.Migrations
{
    /// <inheritdoc />
    public partial class changeUserGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DayOffRequests_Users_UserId",
                table: "DayOffRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "DayOffRequests",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_DayOffRequests_Users_UserId",
                table: "DayOffRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DayOffRequests_Users_UserId",
                table: "DayOffRequests");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "DayOffRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DayOffRequests_Users_UserId",
                table: "DayOffRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
