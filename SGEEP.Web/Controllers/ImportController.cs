using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGEEP.Core.Entities;
using SGEEP.Infrastructure.Data;
using SGEEP.Web.Models.ViewModels;
using SGEEP.Web.Services;
using System.Security.Claims;
using System.Security.Cryptography;

namespace SGEEP.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ImportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditoriaService _auditoria;
        private readonly IEmailService _emailService;

        public ImportController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            AuditoriaService auditoria,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _auditoria = auditoria;
            _emailService = emailService;
        }

        // GET: Import
        public IActionResult Index()
        {
            return View();
        }

        // POST: Import/Importar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(52_428_800)]
        public async Task<IActionResult> Importar(ImportViewModel vm)
        {
            if (vm.Ficheiro == null || vm.Ficheiro.Length == 0)
            {
                TempData["Erro"] = "Por favor selecione um ficheiro.";
                return RedirectToAction(nameof(Index));
            }

            var extensao = Path.GetExtension(vm.Ficheiro.FileName).ToLowerInvariant();
            if (extensao != ".xlsx" && extensao != ".csv")
            {
                TempData["Erro"] = "Formato de ficheiro não suportado. Use XLSX ou CSV.";
                return RedirectToAction(nameof(Index));
            }

            List<string[]> linhas;
            try
            {
                linhas = extensao == ".xlsx"
                    ? LerXlsx(vm.Ficheiro)
                    : LerCsv(vm.Ficheiro);
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao ler o ficheiro: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }

            if (linhas.Count == 0)
            {
                TempData["Erro"] = "O ficheiro está vazio.";
                return RedirectToAction(nameof(Index));
            }

            var resultado = vm.TipoImportacao == "Professores"
                ? await ImportarProfessores(linhas)
                : await ImportarAlunos(linhas);

            return View("Resultado", resultado);
        }

        // GET: Import/DescarregarTemplate?tipo=Professores
        public IActionResult DescarregarTemplate(string tipo)
        {
            using var workbook = new XLWorkbook();

            if (tipo == "Professores")
            {
                var ws = workbook.Worksheets.Add("Professores");
                var headers = new[] { "Nome", "Email", "Telefone", "NIF", "CodigoCurso" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(1, i + 1).Value = headers[i];
                    ws.Cell(1, i + 1).Style.Font.Bold = true;
                    ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
                }
                ws.Cell(2, 1).Value = "João Silva";
                ws.Cell(2, 2).Value = "joao.silva@escola.pt";
                ws.Cell(2, 3).Value = "912345678";
                ws.Cell(2, 4).Value = "123456789";
                ws.Cell(2, 5).Value = "PRG";
                ws.Columns().AdjustToContents();
            }
            else
            {
                var ws = workbook.Worksheets.Add("Alunos");
                var headers = new[] { "Nome", "Email", "Telefone", "NIF", "NumeroAluno", "Turma", "CodigoCurso" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(1, i + 1).Value = headers[i];
                    ws.Cell(1, i + 1).Style.Font.Bold = true;
                    ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
                }
                ws.Cell(2, 1).Value = "Maria Santos";
                ws.Cell(2, 2).Value = "maria.santos@escola.pt";
                ws.Cell(2, 3).Value = "913456789";
                ws.Cell(2, 4).Value = "987654321";
                ws.Cell(2, 5).Value = "2024001";
                ws.Cell(2, 6).Value = "12ºA";
                ws.Cell(2, 7).Value = "PRG";
                ws.Columns().AdjustToContents();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"template_{tipo.ToLower()}.xlsx");
        }

        private async Task<ImportResultadoViewModel> ImportarProfessores(List<string[]> linhas)
        {
            var resultado = new ImportResultadoViewModel
            {
                TipoImportacao = "Professores",
                TotalLinhas = linhas.Count
            };

            var cursos = await _context.Cursos.Where(c => c.Ativo).ToListAsync();

            for (int i = 0; i < linhas.Count; i++)
            {
                var campos = linhas[i];
                var linhaNum = i + 2; // +2 porque linha 1 é header

                if (campos.Length < 5)
                {
                    resultado.Resultados.Add(new ImportLinhaResultado
                    {
                        Linha = linhaNum,
                        Nome = campos.Length > 0 ? campos[0] : "—",
                        Importado = false,
                        Mensagem = "Número insuficiente de colunas. Esperado: Nome, Email, Telefone, NIF, CodigoCurso"
                    });
                    resultado.Erros++;
                    continue;
                }

                var nome = campos[0].Trim();
                var email = campos[1].Trim();
                var telefone = campos[2].Trim();
                var nif = campos[3].Trim();
                var codigoCurso = campos[4].Trim();

                // Validações
                if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(nif))
                {
                    resultado.Resultados.Add(new ImportLinhaResultado
                    {
                        Linha = linhaNum, Nome = nome, Importado = false,
                        Mensagem = "Nome, Email e NIF são obrigatórios."
                    });
                    resultado.Erros++;
                    continue;
                }

                var curso = cursos.FirstOrDefault(c => c.Codigo.Equals(codigoCurso, StringComparison.OrdinalIgnoreCase));
                if (curso == null)
                {
                    resultado.Resultados.Add(new ImportLinhaResultado
                    {
                        Linha = linhaNum, Nome = nome, Importado = false,
                        Mensagem = $"Código de curso '{codigoCurso}' não encontrado."
                    });
                    resultado.Erros++;
                    continue;
                }

                if (await _context.Professores.AnyAsync(p => p.NIF == nif))
                {
                    resultado.Resultados.Add(new ImportLinhaResultado
                    {
                        Linha = linhaNum, Nome = nome, Importado = false,
                        Mensagem = "Já existe um professor com este NIF."
                    });
                    resultado.Erros++;
                    continue;
                }

                if (await _context.Professores.AnyAsync(p => p.Email == email) ||
                    await _userManager.FindByEmailAsync(email) != null)
                {
                    resultado.Resultados.Add(new ImportLinhaResultado
                    {
                        Linha = linhaNum, Nome = nome, Importado = false,
                        Mensagem = "Já existe uma conta com este email."
                    });
                    resultado.Erros++;
                    continue;
                }

                // Criar Identity user
                var user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var passwordTemporaria = GerarPasswordTemporaria();
                var identityResult = await _userManager.CreateAsync(user, passwordTemporaria);
                if (!identityResult.Succeeded)
                {
                    resultado.Resultados.Add(new ImportLinhaResultado
                    {
                        Linha = linhaNum, Nome = nome, Importado = false,
                        Mensagem = string.Join("; ", identityResult.Errors.Select(e => e.Description))
                    });
                    resultado.Erros++;
                    continue;
                }

                await _userManager.AddToRoleAsync(user, "Professor");
                await _userManager.AddClaimAsync(user, new Claim("MustChangePassword", "true"));

                var professor = new Professor
                {
                    Nome = nome,
                    Email = email,
                    Telefone = string.IsNullOrEmpty(telefone) ? null : telefone,
                    NIF = nif,
                    CursoId = curso.Id,
                    ApplicationUserId = user.Id,
                    Ativo = true
                };

                _context.Professores.Add(professor);
                await _context.SaveChangesAsync();

                await _auditoria.RegistarAsync("Importar", "Professor", professor.Id,
                    $"Professor '{professor.Nome}' importado via ficheiro");

                // Enviar email com credenciais
                await _emailService.EnviarAsync(email,
                    "SGEEP — Conta Criada",
                    EmailTemplates.Envolver(
                        EmailTemplates.Saudacao(nome) +
                        "<p>Foi criada uma conta no SGEEP para si.</p>" +
                        EmailTemplates.TabelaCredenciais(email, passwordTemporaria) +
                        EmailTemplates.CaixaAviso("Deverá alterar a password no primeiro acesso.") +
                        EmailTemplates.Assinatura()));

                resultado.Resultados.Add(new ImportLinhaResultado
                {
                    Linha = linhaNum, Nome = nome, Importado = true,
                    Mensagem = $"Professor importado com sucesso. Curso: {curso.Nome}"
                });
                resultado.Sucesso++;
            }

            return resultado;
        }

        private async Task<ImportResultadoViewModel> ImportarAlunos(List<string[]> linhas)
        {
            var resultado = new ImportResultadoViewModel
            {
                TipoImportacao = "Alunos",
                TotalLinhas = linhas.Count
            };

            var cursos = await _context.Cursos.Where(c => c.Ativo).ToListAsync();

            for (int i = 0; i < linhas.Count; i++)
            {
                var campos = linhas[i];
                var linhaNum = i + 2;

                if (campos.Length < 7)
                {
                    resultado.Resultados.Add(new ImportLinhaResultado
                    {
                        Linha = linhaNum,
                        Nome = campos.Length > 0 ? campos[0] : "—",
                        Importado = false,
                        Mensagem = "Número insuficiente de colunas. Esperado: Nome, Email, Telefone, NIF, NumeroAluno, Turma, CodigoCurso"
                    });
                    resultado.Erros++;
                    continue;
                }

                var nome = campos[0].Trim();
                var email = campos[1].Trim();
                var telefone = campos[2].Trim();
                var nif = campos[3].Trim();
                var numeroAluno = campos[4].Trim();
                var turma = campos[5].Trim();
                var codigoCurso = campos[6].Trim();

                if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(nif) || string.IsNullOrEmpty(numeroAluno) ||
                    string.IsNullOrEmpty(turma))
                {
                    resultado.Resultados.Add(new ImportLinhaResultado
                    {
                        Linha = linhaNum, Nome = nome, Importado = false,
                        Mensagem = "Nome, Email, NIF, NumeroAluno e Turma são obrigatórios."
                    });
                    resultado.Erros++;
                    continue;
                }

                var curso = cursos.FirstOrDefault(c => c.Codigo.Equals(codigoCurso, StringComparison.OrdinalIgnoreCase));
                if (curso == null)
                {
                    resultado.Resultados.Add(new ImportLinhaResultado
                    {
                        Linha = linhaNum, Nome = nome, Importado = false,
                        Mensagem = $"Código de curso '{codigoCurso}' não encontrado."
                    });
                    resultado.Erros++;
                    continue;
                }

                if (await _context.Alunos.AnyAsync(a => a.NIF == nif))
                {
                    resultado.Resultados.Add(new ImportLinhaResultado
                    {
                        Linha = linhaNum, Nome = nome, Importado = false,
                        Mensagem = "Já existe um aluno com este NIF."
                    });
                    resultado.Erros++;
                    continue;
                }

                if (await _context.Alunos.AnyAsync(a => a.NumeroAluno == numeroAluno))
                {
                    resultado.Resultados.Add(new ImportLinhaResultado
                    {
                        Linha = linhaNum, Nome = nome, Importado = false,
                        Mensagem = "Já existe um aluno com este número."
                    });
                    resultado.Erros++;
                    continue;
                }

                if (await _context.Alunos.AnyAsync(a => a.Email == email) ||
                    await _userManager.FindByEmailAsync(email) != null)
                {
                    resultado.Resultados.Add(new ImportLinhaResultado
                    {
                        Linha = linhaNum, Nome = nome, Importado = false,
                        Mensagem = "Já existe uma conta com este email."
                    });
                    resultado.Erros++;
                    continue;
                }

                var user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var passwordTemporaria = GerarPasswordTemporaria();
                var identityResult = await _userManager.CreateAsync(user, passwordTemporaria);
                if (!identityResult.Succeeded)
                {
                    resultado.Resultados.Add(new ImportLinhaResultado
                    {
                        Linha = linhaNum, Nome = nome, Importado = false,
                        Mensagem = string.Join("; ", identityResult.Errors.Select(e => e.Description))
                    });
                    resultado.Erros++;
                    continue;
                }

                await _userManager.AddToRoleAsync(user, "Aluno");
                await _userManager.AddClaimAsync(user, new Claim("MustChangePassword", "true"));

                var aluno = new Aluno
                {
                    Nome = nome,
                    Email = email,
                    Telefone = string.IsNullOrEmpty(telefone) ? null : telefone,
                    NIF = nif,
                    NumeroAluno = numeroAluno,
                    Turma = turma,
                    CursoId = curso.Id,
                    ApplicationUserId = user.Id,
                    Ativo = true
                };

                _context.Alunos.Add(aluno);
                await _context.SaveChangesAsync();

                await _auditoria.RegistarAsync("Importar", "Aluno", aluno.Id,
                    $"Aluno '{aluno.Nome}' importado via ficheiro");

                await _emailService.EnviarAsync(email,
                    "SGEEP — Conta Criada",
                    EmailTemplates.Envolver(
                        EmailTemplates.Saudacao(nome) +
                        "<p>Foi criada uma conta no SGEEP para si.</p>" +
                        EmailTemplates.TabelaCredenciais(email, passwordTemporaria) +
                        EmailTemplates.CaixaAviso("Deverá alterar a password no primeiro acesso.") +
                        EmailTemplates.Assinatura()));

                resultado.Resultados.Add(new ImportLinhaResultado
                {
                    Linha = linhaNum, Nome = nome, Importado = true,
                    Mensagem = $"Aluno importado com sucesso. Curso: {curso.Nome}, Turma: {turma}"
                });
                resultado.Sucesso++;
            }

            return resultado;
        }

        private static List<string[]> LerXlsx(IFormFile ficheiro)
        {
            var linhas = new List<string[]>();
            using var stream = ficheiro.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.First();

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

            // Começar na linha 2 (linha 1 é header)
            for (int row = 2; row <= lastRow; row++)
            {
                var campos = new string[lastCol];
                bool linhaVazia = true;
                for (int col = 1; col <= lastCol; col++)
                {
                    campos[col - 1] = ws.Cell(row, col).GetString().Trim();
                    if (!string.IsNullOrEmpty(campos[col - 1]))
                        linhaVazia = false;
                }
                if (!linhaVazia)
                    linhas.Add(campos);
            }

            return linhas;
        }

        private static List<string[]> LerCsv(IFormFile ficheiro)
        {
            var linhas = new List<string[]>();
            using var reader = new StreamReader(ficheiro.OpenReadStream());

            // Saltar header
            reader.ReadLine();

            string? linha;
            while ((linha = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(linha)) continue;

                // Tentar detectar separador (;  ou ,)
                var separador = linha.Contains(';') ? ';' : ',';
                var campos = linha.Split(separador);
                linhas.Add(campos);
            }

            return linhas;
        }

        private static string GerarPasswordTemporaria()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%";
            const string all = upper + lower + digits + special;

            var bytes = RandomNumberGenerator.GetBytes(12);
            var chars = new char[12];
            chars[0] = upper[bytes[0] % upper.Length];
            chars[1] = digits[bytes[1] % digits.Length];
            chars[2] = special[bytes[2] % special.Length];
            for (int i = 3; i < 12; i++)
                chars[i] = all[bytes[i] % all.Length];

            for (int i = chars.Length - 1; i > 0; i--)
            {
                var j = bytes[i % bytes.Length] % (i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars);
        }
    }
}
