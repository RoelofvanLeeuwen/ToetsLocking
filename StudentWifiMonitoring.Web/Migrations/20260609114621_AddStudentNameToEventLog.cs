using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentWifiMonitoring.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentNameToEventLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StudentName",
                table: "Events",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StudentName",
                table: "Events");
        }
    }
}
