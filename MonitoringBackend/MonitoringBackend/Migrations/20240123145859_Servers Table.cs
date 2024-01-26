using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringBackend.Migrations
{
    /// <inheritdoc />
    public partial class ServersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "servers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Host_IP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ssh_User_Login = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ssh_User_Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    User_Id = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "servers");
        }
    }
}
