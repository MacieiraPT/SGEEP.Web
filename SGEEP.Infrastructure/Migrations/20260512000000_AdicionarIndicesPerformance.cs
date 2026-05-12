using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGEEP.Infrastructure.Migrations
{
    /// <summary>
    /// Acrescenta índices em colunas usadas nas verificações de identidade
    /// (Professor.Email, *.ApplicationUserId) e em pesquisas frequentes
    /// (Notificacoes por utilizador + data, RegistoHoras por estágio + data).
    /// Sem estes índices o Postgres faz Seq Scan nestas tabelas em cada pedido.
    /// </summary>
    public partial class AdicionarIndicesPerformance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Professores_Email",
                table: "Professores",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Professores_ApplicationUserId",
                table: "Professores",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_ApplicationUserId",
                table: "Alunos",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_ApplicationUserId",
                table: "Empresas",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_User_Data",
                table: "Notificacoes",
                columns: new[] { "ApplicationUserId", "DataCriacao" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistoHoras_Estagio_Data",
                table: "RegistoHoras",
                columns: new[] { "EstagioId", "Data" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_RegistoHoras_Estagio_Data", table: "RegistoHoras");
            migrationBuilder.DropIndex(name: "IX_Notificacoes_User_Data", table: "Notificacoes");
            migrationBuilder.DropIndex(name: "IX_Empresas_ApplicationUserId", table: "Empresas");
            migrationBuilder.DropIndex(name: "IX_Alunos_ApplicationUserId", table: "Alunos");
            migrationBuilder.DropIndex(name: "IX_Professores_ApplicationUserId", table: "Professores");
            migrationBuilder.DropIndex(name: "IX_Professores_Email", table: "Professores");
        }
    }
}
