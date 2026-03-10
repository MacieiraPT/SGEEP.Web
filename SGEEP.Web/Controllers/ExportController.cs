using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SGEEP.Core.Enums;
using SGEEP.Infrastructure.Data;

namespace SGEEP.Web.Controllers
{
    [Authorize(Roles = "Administrador,Professor")]
    public class ExportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Export/EstagiosExcel
        public async Task<IActionResult> EstagiosExcel()
        {
            var estagios = await _context.Estagios
                .Include(e => e.Aluno).ThenInclude(a => a.Curso)
                .Include(e => e.Empresa)
                .Include(e => e.Professor)
                .OrderByDescending(e => e.DataInicio)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Estágios");

            // Header
            var headers = new[] { "ID", "Aluno", "Curso", "Empresa", "Professor", "Início", "Fim", "Horas Previstas", "Estado" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
            }

            // Data
            int row = 2;
            foreach (var e in estagios)
            {
                ws.Cell(row, 1).Value = e.Id;
                ws.Cell(row, 2).Value = e.Aluno?.Nome ?? "";
                ws.Cell(row, 3).Value = e.Aluno?.Curso?.Nome ?? "";
                ws.Cell(row, 4).Value = e.Empresa?.Nome ?? "";
                ws.Cell(row, 5).Value = e.Professor?.Nome ?? "";
                ws.Cell(row, 6).Value = e.DataInicio.ToString("dd/MM/yyyy");
                ws.Cell(row, 7).Value = e.DataFim.ToString("dd/MM/yyyy");
                ws.Cell(row, 8).Value = e.TotalHorasPrevistas;
                ws.Cell(row, 9).Value = e.Estado.ToString();
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"estagios_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // GET: Export/EstagiosPdf
        public async Task<IActionResult> EstagiosPdf()
        {
            var estagios = await _context.Estagios
                .Include(e => e.Aluno).ThenInclude(a => a.Curso)
                .Include(e => e.Empresa)
                .Include(e => e.Professor)
                .OrderByDescending(e => e.DataInicio)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Text("SGEEP — Lista de Estágios")
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken2);

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);  // Aluno
                            columns.RelativeColumn(1.5f); // Curso
                            columns.RelativeColumn(2);  // Empresa
                            columns.RelativeColumn(1.5f); // Professor
                            columns.RelativeColumn(1);  // Início
                            columns.RelativeColumn(1);  // Fim
                            columns.RelativeColumn(0.8f); // Estado
                        });

                        // Header
                        table.Header(header =>
                        {
                            foreach (var h in new[] { "Aluno", "Curso", "Empresa", "Professor", "Início", "Fim", "Estado" })
                            {
                                header.Cell().Background(Colors.Blue.Lighten4).Padding(5)
                                    .Text(h).SemiBold().FontSize(9);
                            }
                        });

                        // Rows
                        foreach (var e in estagios)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.Aluno?.Nome ?? "");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.Aluno?.Curso?.Nome ?? "");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.Empresa?.Nome ?? "");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.Professor?.Nome ?? "");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.DataInicio.ToString("dd/MM/yyyy"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.DataFim.ToString("dd/MM/yyyy"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(e.Estado.ToString());
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Gerado em ");
                        x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                        x.Span(" — Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            var pdf = document.GeneratePdf();
            return File(pdf, "application/pdf", $"estagios_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // GET: Export/AlunosExcel
        public async Task<IActionResult> AlunosExcel()
        {
            var alunos = await _context.Alunos
                .Include(a => a.Curso)
                .OrderBy(a => a.Nome)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Alunos");

            var headers = new[] { "ID", "Nome", "Email", "NIF", "Curso", "Turma", "Ativo" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
            }

            int row = 2;
            foreach (var a in alunos)
            {
                ws.Cell(row, 1).Value = a.Id;
                ws.Cell(row, 2).Value = a.Nome;
                ws.Cell(row, 3).Value = a.Email;
                ws.Cell(row, 4).Value = a.NIF;
                ws.Cell(row, 5).Value = a.Curso?.Nome ?? "";
                ws.Cell(row, 6).Value = a.Turma ?? "";
                ws.Cell(row, 7).Value = a.Ativo ? "Sim" : "Não";
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"alunos_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // GET: Export/EmpresasExcel
        public async Task<IActionResult> EmpresasExcel()
        {
            var empresas = await _context.Empresas
                .OrderBy(e => e.Nome)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Empresas");

            var headers = new[] { "ID", "Nome", "NIF", "Morada", "Cidade", "Setor", "Tutor", "Email Tutor", "Ativa" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
            }

            int row = 2;
            foreach (var e in empresas)
            {
                ws.Cell(row, 1).Value = e.Id;
                ws.Cell(row, 2).Value = e.Nome;
                ws.Cell(row, 3).Value = e.NIF;
                ws.Cell(row, 4).Value = e.Morada;
                ws.Cell(row, 5).Value = e.Cidade;
                ws.Cell(row, 6).Value = e.Setor ?? "";
                ws.Cell(row, 7).Value = e.NomeTutor;
                ws.Cell(row, 8).Value = e.EmailTutor;
                ws.Cell(row, 9).Value = e.Ativa ? "Sim" : "Não";
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"empresas_{DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}
