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
        private readonly IEmailService _emailService;

        public EmpresasController(
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

            // Enviar email com credenciais de acesso ao tutor
            await _emailService.EnviarAsync(vm.EmailTutor,
                "SGEEP — Conta Criada",
                EmailTemplates.Envolver(
                    $"<p>Caro(a) <strong>{vm.NomeTutor}</strong>,</p>" +
                    $"<p>Foi criada uma conta no SGEEP para a empresa <strong>{vm.Nome}</strong>.</p>" +
                    $"<table style=\"margin:16px 0;border-radius:6px;background:#f8fafc;border:1px solid #e2e8f0;padding:16px 20px;border-collapse:collapse;\">" +
                    $"<tr><td style=\"padding:4px 12px 4px 0;\"><strong>Email:</strong></td><td>{vm.EmailTutor}</td></tr>" +
                    $"<tr><td style=\"padding:4px 12px 4px 0;\"><strong>Password tempor&aacute;ria:</strong></td><td><code style=\"background:#f1f5f9;padding:2px 8px;border-radius:4px;font-size:14px;\">{passwordTemporaria}</code></td></tr>" +
                    $"</table>" +
                    $"<p style=\"color:#b45309;background:#fef3c7;border:1px solid #fde68a;border-radius:6px;padding:12px 16px;\">&#9888; Dever&aacute; alterar a password no primeiro acesso.</p>" +
                    $"<p style=\"margin-top:24px;\">Cumprimentos,<br/><strong>SGEEP</strong></p>"));

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
