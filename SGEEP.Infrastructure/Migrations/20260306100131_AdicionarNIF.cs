using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGEEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarNIF : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NIF",
                table: "Professores",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NIF",
                table: "Alunos",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NIF",
                table: "Professores");

            migrationBuilder.DropColumn(
                name: "NIF",
                table: "Alunos");
        }
    }
}
