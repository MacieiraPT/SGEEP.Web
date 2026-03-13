using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SGEEP.Core.Entities;
using SGEEP.Infrastructure.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using SGEEP.Web.Models;
using SGEEP.Web.Models.ViewModels;
using SGEEP.Web.Services;

namespace SGEEP.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ProfessoresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditoriaService _auditoria;
        private readonly IEmailService _emailService;

        public ProfessoresController(
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

        // GET: Professores
        public async Task<IActionResult> Index(string? pesquisa, int? pagina)
        {
            var query = _context.Professores
                .Include(p => p.Curso)
                .AsQueryable();

            if (!string.IsNullOrEmpty(pesquisa))
                query = query.Where(p =>
                    p.Nome.Contains(pesquisa) ||
                    p.Email.Contains(pesquisa));

            ViewBag.Pesquisa = pesquisa;
            return View(await PaginatedList<Professor>.CreateAsync(
                query.OrderBy(p => p.Nome), pagina ?? 1, 15));
        }

        // GET: Professores/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var professor = await _context.Professores
                .Include(p => p.Curso)
                .Include(p => p.Estagios)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (professor == null) return NotFound();
            return View(professor);
        }

        // GET: Professores/Create
        public async Task<IActionResult> Create()
        {
            return View(new ProfessorViewModel
            {
                Cursos = await GetCursosSelectList()
            });
        }

        // POST: Professores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProfessorViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Cursos = await GetCursosSelectList();
                return View(vm);
            }

            if (await _context.Professores.AnyAsync(p => p.Email == vm.Email))
            {
                ModelState.AddModelError("Email", "Já existe um professor com este email.");
                vm.Cursos = await GetCursosSelectList();
                return View(vm);
            }

            if (await _context.Professores.AnyAsync(p => p.NIF == vm.NIF))
            {
                ModelState.AddModelError("NIF", "Já existe um professor com este NIF.");
                vm.Cursos = await GetCursosSelectList();
                return View(vm);
            }

            if (await _userManager.FindByEmailAsync(vm.Email) != null)
            {
                ModelState.AddModelError("Email", "Já existe uma conta com este email.");
                vm.Cursos = await GetCursosSelectList();
                return View(vm);
            }

            // Criar conta Identity com password temporária aleatória
            var user = new IdentityUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                EmailConfirmed = true
            };

            var passwordTemporaria = GerarPasswordTemporaria();
            var resultado = await _userManager.CreateAsync(user, passwordTemporaria);
            if (!resultado.Succeeded)
            {
                foreach (var erro in resultado.Errors)
                    ModelState.AddModelError("", erro.Description);
                vm.Cursos = await GetCursosSelectList();
                return View(vm);
            }

            await _userManager.AddToRoleAsync(user, "Professor");
            await _userManager.AddClaimAsync(user, new Claim("MustChangePassword", "true"));

            var professor = new Professor
            {
                Nome = vm.Nome,
                Email = vm.Email,
                Telefone = vm.Telefone,
                NIF = vm.NIF,
                CursoId = vm.CursoId,
                ApplicationUserId = user.Id,
                Ativo = true
            };

            _context.Professores.Add(professor);
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Criar", "Professor", professor.Id, $"Professor '{professor.Nome}' criado com email {professor.Email}");

            // Enviar email com credenciais de acesso
            await _emailService.EnviarAsync(vm.Email,
                "SGEEP — Conta Criada",
                $"<p>Caro(a) {vm.Nome},</p>" +
                $"<p>Foi criada uma conta no SGEEP para si.</p>" +
                $"<p><strong>Email:</strong> {vm.Email}<br/>" +
                $"<strong>Password temporária:</strong> {passwordTemporaria}</p>" +
                $"<p>Deverá alterar a password no primeiro acesso.</p>" +
                $"<p>Cumprimentos,<br/>SGEEP</p>");

            TempData["Sucesso"] = $"Professor {professor.Nome} criado! Login: {vm.Email} | Password temporária: {passwordTemporaria}";
            return RedirectToAction(nameof(Index));
        }

        // GET: Professores/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var professor = await _context.Professores.FindAsync(id);
            if (professor == null) return NotFound();

            return View(new ProfessorViewModel
            {
                Id = professor.Id,
                Nome = professor.Nome,
                Email = professor.Email,
                Telefone = professor.Telefone,
                NIF = professor.NIF,
                CursoId = professor.CursoId,
                Ativo = professor.Ativo,
                Cursos = await GetCursosSelectList()
            });
        }

        // POST: Professores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProfessorViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Cursos = await GetCursosSelectList();
                return View(vm);
            }

            var professor = await _context.Professores.FindAsync(id);
            if (professor == null) return NotFound();

            if (await _context.Professores.AnyAsync(p => p.Email == vm.Email && p.Id != id))
            {
                ModelState.AddModelError("Email", "Já existe um professor com este email.");
                vm.Cursos = await GetCursosSelectList();
                return View(vm);
            }

            if (await _context.Professores.AnyAsync(p => p.NIF == vm.NIF && p.Id != id))
            {
                ModelState.AddModelError("NIF", "Já existe um professor com este NIF.");
                vm.Cursos = await GetCursosSelectList();
                return View(vm);
            }

            professor.Nome = vm.Nome;
            professor.Email = vm.Email;
            professor.Telefone = vm.Telefone;
            professor.NIF = vm.NIF;
            professor.CursoId = vm.CursoId;
            professor.Ativo = vm.Ativo;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = $"Professor {professor.Nome} atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Professores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var professor = await _context.Professores
                .Include(p => p.Estagios)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (professor == null) return NotFound();

            var temEstagiosAtivos = professor.Estagios
                .Any(e => e.Estado == SGEEP.Core.Enums.EstadoEstagio.Ativo);

            if (temEstagiosAtivos)
            {
                TempData["Erro"] = $"Não é possível desativar o professor {professor.Nome} porque tem estágios ativos.";
                return RedirectToAction(nameof(Index));
            }

            // Bloquear conta Identity
            if (professor.ApplicationUserId != null)
            {
                var user = await _userManager.FindByIdAsync(professor.ApplicationUserId);
                if (user != null)
                {
                    user.LockoutEnabled = true;
                    user.LockoutEnd = DateTimeOffset.MaxValue;
                    await _userManager.UpdateAsync(user);
                }
            }

            professor.Ativo = false;
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Desativar", "Professor", professor.Id, $"Professor '{professor.Nome}' desativado");

            TempData["Sucesso"] = $"Professor {professor.Nome} desativado com sucesso!";
            return RedirectToAction(nameof(Index));
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

        private async Task<IEnumerable<SelectListItem>> GetCursosSelectList()
        {
            return await _context.Cursos
                .Where(c => c.Ativo)
                .OrderBy(c => c.Nome)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Nome
                })
                .ToListAsync();
        }
    }
}