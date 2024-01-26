using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringBackend.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "User_Id",
                table: "refreshTokens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "User_Id",
                table: "refreshTokens");
        }
    }
}
