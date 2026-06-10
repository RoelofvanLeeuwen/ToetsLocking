using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentWifiMonitoring.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIsTestCompleteToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTestComplete",
                table: "Students",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTestComplete",
                table: "Students");
        }
    }
}
