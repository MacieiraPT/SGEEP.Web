using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGEEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiretorCursoProfessor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DiretorCurso",
                table: "Professores",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiretorCurso",
                table: "Professores");
        }
    }
}
