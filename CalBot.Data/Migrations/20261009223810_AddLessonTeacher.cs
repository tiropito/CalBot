using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonTeacher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Teacher",
                table: "Lessons",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Teacher",
                table: "Lessons");
        }
    }
}
