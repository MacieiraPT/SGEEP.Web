using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGEEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserIdToEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Empresas",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Empresas");
        }
    }
}
