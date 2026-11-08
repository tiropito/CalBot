using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskEndTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "Tasks",
                type: "time without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Tasks");
        }
    }
}
