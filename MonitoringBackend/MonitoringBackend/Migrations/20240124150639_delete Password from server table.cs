using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringBackend.Migrations
{
    /// <inheritdoc />
    public partial class deletePasswordfromservertable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ssh_User_Password",
                table: "servers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ssh_User_Password",
                table: "servers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
