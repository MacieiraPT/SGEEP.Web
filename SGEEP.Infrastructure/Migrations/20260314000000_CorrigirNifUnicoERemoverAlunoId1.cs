using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGEEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CorrigirNifUnicoERemoverAlunoId1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remover a FK e índice fantasma AlunoId1
            migrationBuilder.DropForeignKey(
                name: "FK_Estagios_Alunos_AlunoId1",
                table: "Estagios");

            migrationBuilder.DropIndex(
                name: "IX_Estagios_AlunoId1",
                table: "Estagios");

            migrationBuilder.DropColumn(
                name: "AlunoId1",
                table: "Estagios");

            // Adicionar NIF único para Alunos
            migrationBuilder.CreateIndex(
                name: "IX_Alunos_NIF",
                table: "Alunos",
                column: "NIF",
                unique: true);

            // Adicionar NIF único para Professores
            migrationBuilder.CreateIndex(
                name: "IX_Professores_NIF",
                table: "Professores",
                column: "NIF",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alunos_NIF",
                table: "Alunos");

            migrationBuilder.DropIndex(
                name: "IX_Professores_NIF",
                table: "Professores");

            migrationBuilder.AddColumn<int>(
                name: "AlunoId1",
                table: "Estagios",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estagios_AlunoId1",
                table: "Estagios",
                column: "AlunoId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Estagios_Alunos_AlunoId1",
                table: "Estagios",
                column: "AlunoId1",
                principalTable: "Alunos",
                principalColumn: "Id");
        }
    }
}
