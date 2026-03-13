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
    public class RegistoHorasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditoriaService _auditoria;
        private readonly IEmailService _emailService;

        public RegistoHorasController(
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

        // GET: RegistoHoras/Estagio/5
        public async Task<IActionResult> Index(int estagioId)
        {
            var estagio = await _context.Estagios
                .Include(e => e.Aluno)
                .Include(e => e.Professor)
                .FirstOrDefaultAsync(e => e.Id == estagioId);

            if (estagio == null) return NotFound();

            // Verificar acesso
            if (!await TemAcesso(estagio)) return Forbid();

            var registos = await _context.RegistoHoras
                .Where(r => r.EstagioId == estagioId)
                .OrderByDescending(r => r.Data)
                .ToListAsync();

            ViewBag.Estagio = estagio;
            ViewBag.TotalHorasValidadas = registos
                .Where(r => r.Estado == EstadoHoras.Validado)
                .Sum(r => r.TotalHoras);
            ViewBag.TotalHorasPrevistas = estagio.TotalHorasPrevistas;

            return View(registos);
        }

        // GET: RegistoHoras/Create/5
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
                TempData["Erro"] = "Só é possível registar horas em estágios ativos.";
                return RedirectToAction(nameof(Index), new { estagioId });
            }

            return View(new RegistoHorasViewModel { EstagioId = estagioId });
        }

        // POST: RegistoHoras/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> Create(RegistoHorasViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // Carregar estágio para validações
            var estagio = await _context.Estagios.FindAsync(vm.EstagioId);
            if (estagio == null) return NotFound();

            // Verificar que o aluno autenticado é o dono do estágio
            if (!await TemAcesso(estagio)) return Forbid();

            // Verificar que o estágio está ativo
            if (estagio.Estado != EstadoEstagio.Ativo)
            {
                TempData["Erro"] = "Só é possível registar horas em estágios ativos.";
                return RedirectToAction(nameof(Index), new { estagioId = vm.EstagioId });
            }

            // Validar que a data está dentro do período do estágio
            var dataInicio = DateOnly.FromDateTime(estagio.DataInicio);
            var dataFim = DateOnly.FromDateTime(estagio.DataFim);
            if (vm.Data < dataInicio || vm.Data > dataFim)
            {
                ModelState.AddModelError("Data", $"A data deve estar dentro do período do estágio ({dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}).");
                return View(vm);
            }

            // Validar que a data não é no futuro
            if (vm.Data > DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError("Data", "Não é possível registar horas para uma data futura.");
                return View(vm);
            }

            // Validar horas
            if (vm.HoraSaida <= vm.HoraEntrada)
            {
                ModelState.AddModelError("HoraSaida", "A hora de saída deve ser posterior à hora de entrada.");
                return View(vm);
            }

            // Validar que a pausa não excede o tempo trabalhado
            var tempoTrabalhado = vm.HoraSaida.ToTimeSpan() - vm.HoraEntrada.ToTimeSpan();
            var tempoPausa = vm.HoraPausa?.ToTimeSpan() ?? TimeSpan.Zero;
            if (tempoPausa >= tempoTrabalhado)
            {
                ModelState.AddModelError("HoraPausa", "A duração da pausa não pode ser igual ou superior ao tempo entre entrada e saída.");
                return View(vm);
            }

            // Validar limite máximo de horas diárias (16h)
            var horasEfetivas = (tempoTrabalhado - tempoPausa).TotalHours;
            if (horasEfetivas > 16)
            {
                ModelState.AddModelError("HoraSaida", "O registo não pode exceder 16 horas diárias.");
                return View(vm);
            }

            // Verificar se já existe registo para este dia
            if (await _context.RegistoHoras.AnyAsync(r =>
                r.EstagioId == vm.EstagioId && r.Data == vm.Data))
            {
                ModelState.AddModelError("Data", "Já existe um registo para este dia.");
                return View(vm);
            }

            var registo = new RegistoHoras
            {
                EstagioId = vm.EstagioId,
                Data = vm.Data,
                HoraEntrada = vm.HoraEntrada,
                HoraSaida = vm.HoraSaida,
                HoraPausa = vm.HoraPausa,
                Observacoes = vm.Observacoes,
                Estado = EstadoHoras.Pendente
            };

            _context.RegistoHoras.Add(registo);
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Criar", "RegistoHoras", registo.Id, $"Registo de horas criado para {registo.Data:dd/MM/yyyy} (Estágio #{registo.EstagioId})");

            TempData["Sucesso"] = $"Registo de {vm.Data:dd/MM/yyyy} submetido com sucesso!";
            return RedirectToAction(nameof(Index), new { estagioId = vm.EstagioId });
        }

        // POST: RegistoHoras/Validar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Professor")]
        public async Task<IActionResult> Validar(int id)
        {
            var registo = await _context.RegistoHoras
                .Include(r => r.Estagio)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registo == null) return NotFound();

            if (!await TemAcesso(registo.Estagio)) return Forbid();

            // Verificar que o estágio está ativo
            if (registo.Estagio.Estado != EstadoEstagio.Ativo)
            {
                TempData["Erro"] = "Só é possível validar horas de estágios ativos.";
                return RedirectToAction(nameof(Index), new { estagioId = registo.EstagioId });
            }

            // Verificar que o registo está pendente
            if (registo.Estado != EstadoHoras.Pendente)
            {
                TempData["Erro"] = "Este registo já foi processado.";
                return RedirectToAction(nameof(Index), new { estagioId = registo.EstagioId });
            }

            registo.Estado = EstadoHoras.Validado;
            registo.DataValidacao = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Validar", "RegistoHoras", registo.Id, $"Registo de horas de {registo.Data:dd/MM/yyyy} validado (Estágio #{registo.EstagioId})");

            // Enviar email ao aluno
            var aluno = await _context.Alunos.FirstOrDefaultAsync(a => a.Id == registo.Estagio.AlunoId);
            if (aluno != null && !string.IsNullOrEmpty(aluno.Email))
                await _emailService.EnviarAsync(aluno.Email,
                    "SGEEP — Horas Validadas",
                    EmailTemplates.Envolver(
                        $"<p>Caro(a) <strong>{aluno.Nome}</strong>,</p>" +
                        $"<p>O registo de horas do dia <strong>{registo.Data:dd/MM/yyyy}</strong> foi <span style=\"color:#15803d;font-weight:600;\">validado</span>.</p>" +
                        $"<p style=\"margin-top:24px;\">Cumprimentos,<br/><strong>SGEEP</strong></p>"));

            TempData["Sucesso"] = $"Registo de {registo.Data:dd/MM/yyyy} validado!";
            return RedirectToAction(nameof(Index), new { estagioId = registo.EstagioId });
        }

        // POST: RegistoHoras/Rejeitar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Professor")]
        public async Task<IActionResult> Rejeitar(int id)
        {
            var registo = await _context.RegistoHoras
                .Include(r => r.Estagio)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registo == null) return NotFound();

            if (!await TemAcesso(registo.Estagio)) return Forbid();

            // Verificar que o estágio está ativo
            if (registo.Estagio.Estado != EstadoEstagio.Ativo)
            {
                TempData["Erro"] = "Só é possível rejeitar horas de estágios ativos.";
                return RedirectToAction(nameof(Index), new { estagioId = registo.EstagioId });
            }

            // Verificar que o registo está pendente
            if (registo.Estado != EstadoHoras.Pendente)
            {
                TempData["Erro"] = "Este registo já foi processado.";
                return RedirectToAction(nameof(Index), new { estagioId = registo.EstagioId });
            }

            registo.Estado = EstadoHoras.Rejeitado;
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Rejeitar", "RegistoHoras", registo.Id, $"Registo de horas de {registo.Data:dd/MM/yyyy} rejeitado (Estágio #{registo.EstagioId})");

            // Enviar email ao aluno
            var aluno = await _context.Alunos.FirstOrDefaultAsync(a => a.Id == registo.Estagio.AlunoId);
            if (aluno != null && !string.IsNullOrEmpty(aluno.Email))
                await _emailService.EnviarAsync(aluno.Email,
                    "SGEEP — Horas Rejeitadas",
                    EmailTemplates.Envolver(
                        $"<p>Caro(a) <strong>{aluno.Nome}</strong>,</p>" +
                        $"<p>O registo de horas do dia <strong>{registo.Data:dd/MM/yyyy}</strong> foi <span style=\"color:#dc2626;font-weight:600;\">rejeitado</span>. Verifique e resubmeta.</p>" +
                        $"<p style=\"margin-top:24px;\">Cumprimentos,<br/><strong>SGEEP</strong></p>"));

            TempData["Erro"] = $"Registo de {registo.Data:dd/MM/yyyy} rejeitado.";
            return RedirectToAction(nameof(Index), new { estagioId = registo.EstagioId });
        }

        // POST: RegistoHoras/Apagar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> Apagar(int id)
        {
            var registo = await _context.RegistoHoras
                .Include(r => r.Estagio)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (registo == null) return NotFound();

            // Verificar que o aluno autenticado é o dono do registo
            if (!await TemAcesso(registo.Estagio)) return Forbid();

            // Só pode apagar registos pendentes
            if (registo.Estado != EstadoHoras.Pendente)
            {
                TempData["Erro"] = "Só é possível apagar registos pendentes.";
                return RedirectToAction(nameof(Index), new { estagioId = registo.EstagioId });
            }

            _context.RegistoHoras.Remove(registo);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Registo apagado com sucesso!";
            return RedirectToAction(nameof(Index), new { estagioId = registo.EstagioId });
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