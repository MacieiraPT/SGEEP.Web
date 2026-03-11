using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SGEEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarRegistoAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistosAuditoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DataHora = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Acao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Entidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntidadeId = table.Column<int>(type: "integer", nullable: true),
                    Detalhes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UtilizadorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    EnderecoIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistosAuditoria", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistosAuditoria_DataHora",
                table: "RegistosAuditoria",
                column: "DataHora");

            migrationBuilder.CreateIndex(
                name: "IX_RegistosAuditoria_ApplicationUserId",
                table: "RegistosAuditoria",
                column: "ApplicationUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistosAuditoria");
        }
    }
}
