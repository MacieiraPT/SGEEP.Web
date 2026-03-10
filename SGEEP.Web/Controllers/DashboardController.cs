using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGEEP.Core.Enums;
using SGEEP.Infrastructure.Data;

namespace SGEEP.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalAlunos = await _context.Alunos.CountAsync(a => a.Ativo);
            ViewBag.EstagiosAtivos = await _context.Estagios.CountAsync(e => e.Estado == EstadoEstagio.Ativo);
            ViewBag.TotalEmpresas = await _context.Empresas.CountAsync(e => e.Ativa);
            ViewBag.RelatoriosPendentes = await _context.Relatorios.CountAsync(r => r.Estado == EstadoRelatorio.Submetido);
            ViewBag.HorasPorValidar = await _context.RegistoHoras.CountAsync(r => r.Estado == EstadoHoras.Pendente);
            ViewBag.EstagiosPendentes = await _context.Estagios.CountAsync(e => e.Estado == EstadoEstagio.Pendente);

            ViewBag.UltimosEstagios = await _context.Estagios
                .Include(e => e.Aluno)
                .Include(e => e.Empresa)
                .OrderByDescending(e => e.Id)
                .Take(5)
                .ToListAsync();

            return View();
        }

        [Authorize(Roles = "Professor")]
        public async Task<IActionResult> Professor()
        {
            var user = await _userManager.GetUserAsync(User);
            var professor = await _context.Professores
                .FirstOrDefaultAsync(p => p.ApplicationUserId == user!.Id);

            if (professor == null) return RedirectToAction("Index", "Home");

            ViewBag.Professor = professor;
            ViewBag.AlunosNoCurso = await _context.Alunos
                .CountAsync(a => a.CursoId == professor.CursoId && a.Ativo);
            ViewBag.EstagiosAtivos = await _context.Estagios
                .CountAsync(e => e.ProfessorId == professor.Id && e.Estado == EstadoEstagio.Ativo);
            ViewBag.HorasPorValidar = await _context.RegistoHoras
                .Include(r => r.Estagio)
                .CountAsync(r => r.Estagio.ProfessorId == professor.Id && r.Estado == EstadoHoras.Pendente);
            ViewBag.RelatoriosPorAvaliar = await _context.Relatorios
                .Include(r => r.Estagio)
                .CountAsync(r => r.Estagio.ProfessorId == professor.Id && r.Estado == EstadoRelatorio.Submetido);

            ViewBag.UltimosEstagios = await _context.Estagios
                .Include(e => e.Aluno)
                .Include(e => e.Empresa)
                .Where(e => e.ProfessorId == professor.Id)
                .OrderByDescending(e => e.Id)
                .Take(5)
                .ToListAsync();

            return View();
        }

        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> Aluno()
        {
            var user = await _userManager.GetUserAsync(User);
            var aluno = await _context.Alunos
                .Include(a => a.Curso)
                .FirstOrDefaultAsync(a => a.ApplicationUserId == user!.Id);

            if (aluno == null) return RedirectToAction("Index", "Home");

            var estagio = await _context.Estagios
                .Include(e => e.Empresa)
                .Include(e => e.RegistoHoras)
                .Include(e => e.Relatorios)
                .Where(e => e.AlunoId == aluno.Id)
                .OrderByDescending(e => e.Id)
                .FirstOrDefaultAsync();

            ViewBag.Aluno = aluno;
            ViewBag.Estagio = estagio;

            if (estagio != null)
            {
                ViewBag.HorasValidadas = estagio.RegistoHoras
                    .Where(r => r.Estado == EstadoHoras.Validado)
                    .Sum(r => r.TotalHoras);
                ViewBag.HorasPendentes = estagio.RegistoHoras
                    .Count(r => r.Estado == EstadoHoras.Pendente);
                ViewBag.RelatoriosSubmetidos = estagio.Relatorios.Count;
                ViewBag.RelatoriosAprovados = estagio.Relatorios
                    .Count(r => r.Estado == EstadoRelatorio.Aprovado);
            }

            return View();
        }

        [Authorize(Roles = "Empresa")]
        public async Task<IActionResult> Empresa()
        {
            var user = await _userManager.GetUserAsync(User);
            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.ApplicationUserId == user!.Id);

            if (empresa == null) return RedirectToAction("Index", "Home");

            ViewBag.Empresa = empresa;
            ViewBag.EstagiariosAtivos = await _context.Estagios
                .CountAsync(e => e.EmpresaId == empresa.Id && e.Estado == EstadoEstagio.Ativo);
            ViewBag.HorasPorValidar = await _context.RegistoHoras
                .Include(r => r.Estagio)
                .CountAsync(r => r.Estagio.EmpresaId == empresa.Id && r.Estado == EstadoHoras.Pendente);
            ViewBag.EstagiosConcluidos = await _context.Estagios
                .CountAsync(e => e.EmpresaId == empresa.Id && e.Estado == EstadoEstagio.Concluido);

            ViewBag.UltimosEstagios = await _context.Estagios
                .Include(e => e.Aluno)
                .Include(e => e.Professor)
                .Where(e => e.EmpresaId == empresa.Id)
                .OrderByDescending(e => e.Id)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }
}