using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGEEP.Infrastructure.Data;

namespace SGEEP.Web.Controllers
{
    [Authorize(Roles = "Aluno")]
    public class AlunoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AlunoController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> MeuEstagio()
        {
            var user = await _userManager.GetUserAsync(User);
            var aluno = await _context.Alunos
                .Include(a => a.Curso)
                .Include(a => a.Estagios)
                    .ThenInclude(e => e.Empresa)
                .Include(a => a.Estagios)
                    .ThenInclude(e => e.Professor)
                .Include(a => a.Estagios)
                    .ThenInclude(e => e.RegistoHoras)
                .Include(a => a.Estagios)
                    .ThenInclude(e => e.Relatorios)
                .FirstOrDefaultAsync(a => a.ApplicationUserId == user!.Id);

            if (aluno == null)
            {
                TempData["Erro"] = "Perfil de aluno não encontrado.";
                return RedirectToAction("Index", "Home");
            }

            return View(aluno);
        }
    }
}