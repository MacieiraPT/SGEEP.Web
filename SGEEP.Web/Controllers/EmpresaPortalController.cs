using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGEEP.Core.Enums;
using SGEEP.Infrastructure.Data;
using SGEEP.Web.Services;

namespace SGEEP.Web.Controllers
{
    [Authorize(Roles = "Empresa")]
    public class EmpresaPortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly NotificacaoService _notificacaoService;
        private readonly AuditoriaService _auditoria;
        private readonly IEmailService _emailService;

        public EmpresaPortalController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            NotificacaoService notificacaoService,
            AuditoriaService auditoria,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _notificacaoService = notificacaoService;
            _auditoria = auditoria;
            _emailService = emailService;
        }

        private async Task<int?> GetEmpresaIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.ApplicationUserId == user!.Id && e.Ativa);
            return empresa?.Id;
        }

        // GET: EmpresaPortal/MeusEstagios
        public async Task<IActionResult> MeusEstagios()
        {
            var empresaId = await GetEmpresaIdAsync();
            if (empresaId == null) return RedirectToAction("Index", "Home");

            var estagios = await _context.Estagios
                .Include(e => e.Aluno)
                .Include(e => e.Professor)
                .Include(e => e.RegistoHoras)
                .Where(e => e.EmpresaId == empresaId)
                .OrderByDescending(e => e.Id)
                .ToListAsync();

            return View(estagios);
        }

        // GET: EmpresaPortal/RegistoHoras/5
        public async Task<IActionResult> RegistoHoras(int id)
        {
            var empresaId = await GetEmpresaIdAsync();
            if (empresaId == null) return RedirectToAction("Index", "Home");

            var estagio = await _context.Estagios
                .Include(e => e.Aluno)
                .Include(e => e.RegistoHoras.OrderByDescending(r => r.Data))
                .FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId);

            if (estagio == null) return NotFound();

            return View(estagio);
        }

        // POST: EmpresaPortal/ValidarHoras/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarHoras(int id)
        {
            var empresaId = await GetEmpresaIdAsync();
            if (empresaId == null) return RedirectToAction("Index", "Home");

            var registo = await _context.RegistoHoras
                .Include(r => r.Estagio).ThenInclude(e => e.Aluno)
                .FirstOrDefaultAsync(r => r.Id == id && r.Estagio.EmpresaId == empresaId);

            if (registo == null) return NotFound();

            // Verificar que o estágio está ativo
            if (registo.Estagio.Estado != EstadoEstagio.Ativo)
            {
                TempData["Erro"] = "Só é possível validar horas de estágios ativos.";
                return RedirectToAction("RegistoHoras", new { id = registo.EstagioId });
            }

            // Verificar que o registo está pendente
            if (registo.Estado != EstadoHoras.Pendente)
            {
                TempData["Erro"] = "Este registo já foi processado.";
                return RedirectToAction("RegistoHoras", new { id = registo.EstagioId });
            }

            registo.Estado = EstadoHoras.Validado;
            registo.DataValidacao = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("ValidarHoras", "RegistoHoras", registo.Id, $"Empresa validou horas de {registo.Data:dd/MM/yyyy} (Estágio #{registo.EstagioId})");

            // Notificar aluno
            if (!string.IsNullOrEmpty(registo.Estagio.Aluno?.ApplicationUserId))
                await _notificacaoService.CriarAsync(registo.Estagio.Aluno.ApplicationUserId,
                    "Horas Validadas", $"O registo de horas do dia {registo.Data:dd/MM/yyyy} foi validado.");

            // Enviar email ao aluno
            if (!string.IsNullOrEmpty(registo.Estagio.Aluno?.Email))
                await _emailService.EnviarAsync(registo.Estagio.Aluno.Email,
                    "SGEEP — Horas Validadas",
                    EmailTemplates.Envolver(
                        $"<p>Caro(a) <strong>{registo.Estagio.Aluno.Nome}</strong>,</p>" +
                        $"<p>O registo de horas do dia <strong>{registo.Data:dd/MM/yyyy}</strong> foi <span style=\"color:#15803d;font-weight:600;\">validado</span> pela empresa.</p>" +
                        $"<p style=\"margin-top:24px;\">Cumprimentos,<br/><strong>SGEEP</strong></p>"));

            TempData["Sucesso"] = "Registo de horas validado com sucesso.";
            return RedirectToAction("RegistoHoras", new { id = registo.EstagioId });
        }

        // POST: EmpresaPortal/RejeitarHoras/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejeitarHoras(int id)
        {
            var empresaId = await GetEmpresaIdAsync();
            if (empresaId == null) return RedirectToAction("Index", "Home");

            var registo = await _context.RegistoHoras
                .Include(r => r.Estagio).ThenInclude(e => e.Aluno)
                .FirstOrDefaultAsync(r => r.Id == id && r.Estagio.EmpresaId == empresaId);

            if (registo == null) return NotFound();

            // Verificar que o estágio está ativo
            if (registo.Estagio.Estado != EstadoEstagio.Ativo)
            {
                TempData["Erro"] = "Só é possível rejeitar horas de estágios ativos.";
                return RedirectToAction("RegistoHoras", new { id = registo.EstagioId });
            }

            // Verificar que o registo está pendente
            if (registo.Estado != EstadoHoras.Pendente)
            {
                TempData["Erro"] = "Este registo já foi processado.";
                return RedirectToAction("RegistoHoras", new { id = registo.EstagioId });
            }

            registo.Estado = EstadoHoras.Rejeitado;
            registo.DataValidacao = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("RejeitarHoras", "RegistoHoras", registo.Id, $"Empresa rejeitou horas de {registo.Data:dd/MM/yyyy} (Estágio #{registo.EstagioId})");

            // Notificar aluno
            if (!string.IsNullOrEmpty(registo.Estagio.Aluno?.ApplicationUserId))
                await _notificacaoService.CriarAsync(registo.Estagio.Aluno.ApplicationUserId,
                    "Horas Rejeitadas", $"O registo de horas do dia {registo.Data:dd/MM/yyyy} foi rejeitado. Verifique e resubmeta.");

            // Enviar email ao aluno
            if (!string.IsNullOrEmpty(registo.Estagio.Aluno?.Email))
                await _emailService.EnviarAsync(registo.Estagio.Aluno.Email,
                    "SGEEP — Horas Rejeitadas",
                    EmailTemplates.Envolver(
                        $"<p>Caro(a) <strong>{registo.Estagio.Aluno.Nome}</strong>,</p>" +
                        $"<p>O registo de horas do dia <strong>{registo.Data:dd/MM/yyyy}</strong> foi <span style=\"color:#dc2626;font-weight:600;\">rejeitado</span> pela empresa. Verifique e resubmeta.</p>" +
                        $"<p style=\"margin-top:24px;\">Cumprimentos,<br/><strong>SGEEP</strong></p>"));

            TempData["Sucesso"] = "Registo de horas rejeitado.";
            return RedirectToAction("RegistoHoras", new { id = registo.EstagioId });
        }
    }
}
