using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGEEP.Core.Entities;
using SGEEP.Core.Enums;
using SGEEP.Infrastructure.Data;
using SGEEP.Web.Models.ViewModels;

namespace SGEEP.Web.Controllers
{
    [Authorize]
    public class RegistoHorasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RegistoHorasController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            // Validar horas
            if (vm.HoraSaida <= vm.HoraEntrada)
            {
                ModelState.AddModelError("HoraSaida", "A hora de saída deve ser posterior à hora de entrada.");
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

            registo.Estado = EstadoHoras.Validado;
            registo.DataValidacao = DateTime.UtcNow;
            await _context.SaveChangesAsync();

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

            registo.Estado = EstadoHoras.Rejeitado;
            await _context.SaveChangesAsync();

            TempData["Erro"] = $"Registo de {registo.Data:dd/MM/yyyy} rejeitado.";
            return RedirectToAction(nameof(Index), new { estagioId = registo.EstagioId });
        }

        // POST: RegistoHoras/Apagar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> Apagar(int id)
        {
            var registo = await _context.RegistoHoras.FindAsync(id);
            if (registo == null) return NotFound();

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