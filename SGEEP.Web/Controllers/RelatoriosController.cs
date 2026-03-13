using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGEEP.Core.Entities;
using SGEEP.Core.Enums;
using SGEEP.Infrastructure.Data;
using SGEEP.Web.Models.ViewModels;
using SGEEP.Web.Services;

namespace SGEEP.Web.Controllers
{
    [Authorize]
    public class RelatoriosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly AuditoriaService _auditoria;
        private readonly IEmailService _emailService;

        public RelatoriosController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment environment,
            AuditoriaService auditoria,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _auditoria = auditoria;
            _emailService = emailService;
        }

        // GET: Relatorios/Index/5 (por estágio)
        public async Task<IActionResult> Index(int estagioId)
        {
            var estagio = await _context.Estagios
                .Include(e => e.Aluno)
                .Include(e => e.Professor)
                .FirstOrDefaultAsync(e => e.Id == estagioId);

            if (estagio == null) return NotFound();
            if (!await TemAcesso(estagio)) return Forbid();

            var relatorios = await _context.Relatorios
                .Where(r => r.EstagioId == estagioId)
                .OrderByDescending(r => r.DataSubmissao)
                .ToListAsync();

            ViewBag.Estagio = estagio;
            return View(relatorios);
        }

        // GET: Relatorios/Create/5
        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> Create(int estagioId)
        {
            var estagio = await _context.Estagios
                .Include(e => e.Aluno)
                .FirstOrDefaultAsync(e => e.Id == estagioId);

            if (estagio == null) return NotFound();
            if (!await TemAcesso(estagio)) return Forbid();

            if (estagio.Estado != EstadoEstagio.Ativo)
            {
                TempData["Erro"] = "Só é possível submeter relatórios em estágios ativos.";
                return RedirectToAction(nameof(Index), new { estagioId });
            }

            return View(new RelatorioViewModel { EstagioId = estagioId });
        }

        // POST: Relatorios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> Create(RelatorioViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // Verificar propriedade e estado do estágio
            var estagio = await _context.Estagios.FindAsync(vm.EstagioId);
            if (estagio == null) return NotFound();
            if (!await TemAcesso(estagio)) return Forbid();

            if (estagio.Estado != EstadoEstagio.Ativo)
            {
                TempData["Erro"] = "Só é possível submeter relatórios em estágios ativos.";
                return RedirectToAction(nameof(Index), new { estagioId = vm.EstagioId });
            }

            string? ficheiroPath = null;

            if (vm.Ficheiro != null && vm.Ficheiro.Length > 0)
            {
                // Validar tipo de ficheiro
                var extensoesPermitidas = new[] { ".pdf", ".docx", ".doc" };
                var extensao = Path.GetExtension(vm.Ficheiro.FileName).ToLower();

                if (!extensoesPermitidas.Contains(extensao))
                {
                    ModelState.AddModelError("Ficheiro", "Apenas são permitidos ficheiros PDF ou DOCX.");
                    return View(vm);
                }

                // Validar tamanho (máx 10MB)
                if (vm.Ficheiro.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("Ficheiro", "O ficheiro não pode exceder 10MB.");
                    return View(vm);
                }

                // Validar magic bytes do ficheiro
                using (var checkStream = vm.Ficheiro.OpenReadStream())
                {
                    var headerBytes = new byte[4];
                    await checkStream.ReadAsync(headerBytes, 0, 4);

                    if (!Helpers.ValidadorFicheiro.ValidarMagicBytes(headerBytes, extensao))
                    {
                        ModelState.AddModelError("Ficheiro", "O conteúdo do ficheiro não corresponde à extensão.");
                        return View(vm);
                    }
                }

                // Guardar ficheiro
                var nomeUnico = $"{Guid.NewGuid()}{extensao}";
                var pasta = Path.Combine(_environment.WebRootPath, "uploads", "relatorios");
                Directory.CreateDirectory(pasta);
                var caminhoCompleto = Path.Combine(pasta, nomeUnico);

                using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                    await vm.Ficheiro.CopyToAsync(stream);

                ficheiroPath = $"/uploads/relatorios/{nomeUnico}";
            }

            var relatorio = new Relatorio
            {
                EstagioId = vm.EstagioId,
                Titulo = vm.Titulo,
                Descricao = vm.Descricao,
                FicheiroPath = ficheiroPath,
                Estado = EstadoRelatorio.Submetido,
                DataSubmissao = DateTime.UtcNow
            };

            _context.Relatorios.Add(relatorio);
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Criar", "Relatorio", relatorio.Id, $"Relatório '{relatorio.Titulo}' submetido (Estágio #{relatorio.EstagioId})");

            TempData["Sucesso"] = "Relatório submetido com sucesso!";
            return RedirectToAction(nameof(Index), new { estagioId = vm.EstagioId });
        }

        // GET: Relatorios/Avaliar/5
        [Authorize(Roles = "Administrador,Professor")]
        public async Task<IActionResult> Avaliar(int id)
        {
            var relatorio = await _context.Relatorios
                .Include(r => r.Estagio).ThenInclude(e => e.Aluno)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (relatorio == null) return NotFound();

            if (!await TemAcesso(relatorio.Estagio)) return Forbid();

            return View(new RelatorioViewModel
            {
                Id = relatorio.Id,
                EstagioId = relatorio.EstagioId,
                Titulo = relatorio.Titulo,
                Descricao = relatorio.Descricao,
                FicheiroPath = relatorio.FicheiroPath,
                ComentarioProfessor = relatorio.ComentarioProfessor,
                Estado = relatorio.Estado
            });
        }

        // POST: Relatorios/Avaliar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Professor")]
        public async Task<IActionResult> Avaliar(RelatorioViewModel vm, string acao)
        {
            var relatorio = await _context.Relatorios
                .Include(r => r.Estagio)
                .FirstOrDefaultAsync(r => r.Id == vm.Id);
            if (relatorio == null) return NotFound();

            if (!await TemAcesso(relatorio.Estagio)) return Forbid();

            relatorio.ComentarioProfessor = vm.ComentarioProfessor;
            relatorio.DataAvaliacao = DateTime.UtcNow;
            relatorio.Estado = acao == "aprovar"
                ? EstadoRelatorio.Aprovado
                : EstadoRelatorio.Rejeitado;

            await _context.SaveChangesAsync();

            var acaoAudit = acao == "aprovar" ? "Aprovar" : "Rejeitar";
            await _auditoria.RegistarAsync(acaoAudit, "Relatorio", relatorio.Id, $"Relatório '{relatorio.Titulo}' {acaoAudit.ToLower()}do (Estágio #{relatorio.EstagioId})");

            // Enviar email ao aluno sobre o resultado da avaliação do relatório
            var estagioCom = await _context.Estagios
                .Include(e => e.Aluno)
                .FirstOrDefaultAsync(e => e.Id == relatorio.EstagioId);

            if (estagioCom?.Aluno != null && !string.IsNullOrEmpty(estagioCom.Aluno.Email))
            {
                var estadoTexto = acao == "aprovar" ? "aprovado" : "rejeitado";
                var corEstado = acao == "aprovar" ? "#15803d" : "#dc2626";
                await _emailService.EnviarAsync(estagioCom.Aluno.Email,
                    $"SGEEP — Relatório {char.ToUpper(estadoTexto[0])}{estadoTexto[1..]}",
                    EmailTemplates.Envolver(
                        $"<p>Caro(a) <strong>{estagioCom.Aluno.Nome}</strong>,</p>" +
                        $"<p>O seu relat&oacute;rio <strong>{relatorio.Titulo}</strong> foi <span style=\"color:{corEstado};font-weight:600;\">{estadoTexto}</span>.</p>" +
                        (string.IsNullOrEmpty(vm.ComentarioProfessor) ? "" :
                            $"<div style=\"background:#f8fafc;border-left:4px solid #1d4ed8;padding:12px 16px;margin:16px 0;border-radius:0 6px 6px 0;\"><strong>Coment&aacute;rio:</strong> {vm.ComentarioProfessor}</div>") +
                        $"<p style=\"margin-top:24px;\">Cumprimentos,<br/><strong>SGEEP</strong></p>"));
            }

            TempData["Sucesso"] = acao == "aprovar"
                ? "Relatório aprovado com sucesso!"
                : "Relatório rejeitado.";

            return RedirectToAction(nameof(Index), new { estagioId = relatorio.EstagioId });
        }

        // GET: Relatorios/Download/5
        public async Task<IActionResult> Download(int id)
        {
            var relatorio = await _context.Relatorios
                .Include(r => r.Estagio)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (relatorio == null || relatorio.FicheiroPath == null)
                return NotFound();

            if (!await TemAcesso(relatorio.Estagio)) return Forbid();

            // Resolve the physical path and ensure it stays within the uploads directory
            var uploadsRoot = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "uploads", "relatorios"));
            var normalised = relatorio.FicheiroPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var caminhoFisico = Path.GetFullPath(Path.Combine(_environment.WebRootPath, normalised));

            if (!caminhoFisico.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return BadRequest();

            if (!System.IO.File.Exists(caminhoFisico))
                return NotFound();

            var extensao = Path.GetExtension(caminhoFisico).ToLower();
            var contentType = extensao == ".pdf" ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            return PhysicalFile(caminhoFisico, contentType, $"{relatorio.Titulo}{extensao}");
        }

        private async Task<bool> TemAcesso(Estagio estagio)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;
            if (User.IsInRole("Administrador")) return true;

            if (User.IsInRole("Professor"))
            {
                var professor = await _context.Professores
                    .FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id);
                return professor?.Id == estagio.ProfessorId;
            }

            if (User.IsInRole("Aluno"))
            {
                var aluno = await _context.Alunos
                    .FirstOrDefaultAsync(a => a.ApplicationUserId == user.Id);
                return aluno?.Id == estagio.AlunoId;
            }

            return false;
        }
    }
}