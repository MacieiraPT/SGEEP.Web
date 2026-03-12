using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGEEP.Core.Entities;
using SGEEP.Infrastructure.Data;
using SGEEP.Web.Models;
using SGEEP.Web.Models.ViewModels;
using SGEEP.Web.Services;

namespace SGEEP.Web.Controllers
{
    [Authorize(Roles = "Administrador,Professor")]
    public class EmpresasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditoriaService _auditoria;

        public EmpresasController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            AuditoriaService auditoria)
        {
            _context = context;
            _userManager = userManager;
            _auditoria = auditoria;
        }

        // GET: Empresas
        public async Task<IActionResult> Index(string? pesquisa, int? pagina)
        {
            var query = _context.Empresas.AsQueryable();

            if (!string.IsNullOrEmpty(pesquisa))
                query = query.Where(e =>
                    e.Nome.Contains(pesquisa) ||
                    e.NIF.Contains(pesquisa) ||
                    (e.Cidade != null && e.Cidade.Contains(pesquisa)));

            ViewBag.Pesquisa = pesquisa;
            return View(await PaginatedList<Empresa>.CreateAsync(
                query.OrderBy(e => e.Nome), pagina ?? 1, 15));
        }

        // GET: Empresas/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var empresa = await _context.Empresas
                .Include(e => e.Estagios)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (empresa == null) return NotFound();
            return View(empresa);
        }

        // GET: Empresas/Create
        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            return View(new EmpresaViewModel());
        }

        // POST: Empresas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create(EmpresaViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            if (await _context.Empresas.AnyAsync(e => e.NIF == vm.NIF))
            {
                ModelState.AddModelError("NIF", "Já existe uma empresa com este NIF.");
                return View(vm);
            }

            if (await _userManager.FindByEmailAsync(vm.EmailTutor) != null)
            {
                ModelState.AddModelError("EmailTutor", "Já existe uma conta com este email.");
                return View(vm);
            }

            // Criar conta Identity para o Tutor com password temporária aleatória
            var user = new IdentityUser
            {
                UserName = vm.EmailTutor,
                Email = vm.EmailTutor,
                EmailConfirmed = true
            };

            var passwordTemporaria = GerarPasswordTemporaria();
            var resultado = await _userManager.CreateAsync(user, passwordTemporaria);
            if (!resultado.Succeeded)
            {
                foreach (var erro in resultado.Errors)
                    ModelState.AddModelError("", erro.Description);
                return View(vm);
            }

            await _userManager.AddToRoleAsync(user, "Empresa");
            await _userManager.AddClaimAsync(user, new Claim("MustChangePassword", "true"));

            var empresa = new Empresa
            {
                Nome = vm.Nome,
                NIF = vm.NIF,
                Morada = vm.Morada,
                Cidade = vm.Cidade,
                Setor = vm.Setor,
                NomeTutor = vm.NomeTutor,
                EmailTutor = vm.EmailTutor,
                TelefoneTutor = vm.TelefoneTutor,
                Ativa = vm.Ativa,
                ApplicationUserId = user.Id
            };

            _context.Empresas.Add(empresa);
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Criar", "Empresa", empresa.Id, $"Empresa '{empresa.Nome}' criada");

            TempData["Sucesso"] = $"Empresa {empresa.Nome} criada! Login Tutor: {vm.EmailTutor} | Password temporária: {passwordTemporaria}";
            return RedirectToAction(nameof(Index));
        }

        // GET: Empresas/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);
            if (empresa == null) return NotFound();

            return View(new EmpresaViewModel
            {
                Id = empresa.Id,
                Nome = empresa.Nome,
                NIF = empresa.NIF,
                Morada = empresa.Morada,
                Cidade = empresa.Cidade,
                Setor = empresa.Setor,
                NomeTutor = empresa.NomeTutor,
                EmailTutor = empresa.EmailTutor,
                TelefoneTutor = empresa.TelefoneTutor,
                Ativa = empresa.Ativa
            });
        }

        // POST: Empresas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, EmpresaViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var empresa = await _context.Empresas.FindAsync(id);
            if (empresa == null) return NotFound();

            if (await _context.Empresas.AnyAsync(e => e.NIF == vm.NIF && e.Id != id))
            {
                ModelState.AddModelError("NIF", "Já existe uma empresa com este NIF.");
                return View(vm);
            }

            empresa.Nome = vm.Nome;
            empresa.NIF = vm.NIF;
            empresa.Morada = vm.Morada;
            empresa.Cidade = vm.Cidade;
            empresa.Setor = vm.Setor;
            empresa.NomeTutor = vm.NomeTutor;
            empresa.EmailTutor = vm.EmailTutor;
            empresa.TelefoneTutor = vm.TelefoneTutor;
            empresa.Ativa = vm.Ativa;

            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Editar", "Empresa", empresa.Id, $"Empresa '{empresa.Nome}' editada");

            TempData["Sucesso"] = $"Empresa {empresa.Nome} atualizada com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Empresas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var empresa = await _context.Empresas
                .Include(e => e.Estagios)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (empresa == null) return NotFound();

            var temEstagiosAtivos = empresa.Estagios
                .Any(e => e.Estado == SGEEP.Core.Enums.EstadoEstagio.Ativo);

            if (temEstagiosAtivos)
            {
                TempData["Erro"] = $"Não é possível desativar a empresa {empresa.Nome} porque tem estágios ativos.";
                return RedirectToAction(nameof(Index));
            }

            // Bloquear conta Identity do Tutor
            if (empresa.ApplicationUserId != null)
            {
                var user = await _userManager.FindByIdAsync(empresa.ApplicationUserId);
                if (user != null)
                {
                    user.LockoutEnabled = true;
                    user.LockoutEnd = DateTimeOffset.MaxValue;
                    await _userManager.UpdateAsync(user);
                }
            }

            empresa.Ativa = false;
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Desativar", "Empresa", empresa.Id, $"Empresa '{empresa.Nome}' desativada");

            TempData["Sucesso"] = $"Empresa {empresa.Nome} desativada com sucesso!";
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
    }
}
