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
    [Authorize(Roles = "Administrador,Professor")]
    public class AlunosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditoriaService _auditoria;
        private readonly IEmailService _emailService;

        public AlunosController(
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

        // GET: Alunos
        public async Task<IActionResult> Index(string? pesquisa, int? cursoId, int? pagina)
        {
            var query = _context.Alunos
                .Include(a => a.Curso)
                .AsQueryable();

            if (User.IsInRole("Professor"))
            {
                var userEmail = User.Identity!.Name;
                var professor = await _context.Professores
                    .FirstOrDefaultAsync(p => p.Email == userEmail && p.Ativo);

                if (professor == null)
                {
                    TempData["Erro"] = "Perfil de professor não encontrado.";
                    return View(new PaginatedList<Aluno>(new List<Aluno>(), 0, 1, 15));
                }

                query = query.Where(a => a.CursoId == professor.CursoId);
                ViewBag.CursoFixo = true;
            }

            if (!string.IsNullOrEmpty(pesquisa))
                query = query.Where(a =>
                    a.Nome.Contains(pesquisa) ||
                    a.NumeroAluno.Contains(pesquisa) ||
                    a.Email.Contains(pesquisa));

            if (cursoId.HasValue && !User.IsInRole("Professor"))
                query = query.Where(a => a.CursoId == cursoId);

            ViewBag.Pesquisa = pesquisa;
            ViewBag.CursoId = cursoId;
            ViewBag.Cursos = new SelectList(
                await _context.Cursos.Where(c => c.Ativo).ToListAsync(), "Id", "Nome");

            return View(await PaginatedList<Aluno>.CreateAsync(
                query.OrderBy(a => a.Nome), pagina ?? 1, 15));
        }

        // GET: Alunos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var aluno = await _context.Alunos
                .Include(a => a.Curso)
                .Include(a => a.Estagios)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (aluno == null) return NotFound();

            // Professor só pode ver alunos do seu curso
            if (User.IsInRole("Professor"))
            {
                var userEmail = User.Identity!.Name;
                var professor = await _context.Professores
                    .FirstOrDefaultAsync(p => p.Email == userEmail && p.Ativo);

                if (professor == null || aluno.CursoId != professor.CursoId)
                    return Forbid();
            }

            return View(aluno);
        }

        // GET: Alunos/Create
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create()
        {
            return View(new AlunoViewModel
            {
                Cursos = await GetCursosSelectList()
            });
        }

        // POST: Alunos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create(AlunoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Cursos = await GetCursosSelectList();
                return View(vm);
            }

            // Verificar duplicados
            if (await _context.Alunos.AnyAsync(a => a.NumeroAluno == vm.NumeroAluno))
            {
                ModelState.AddModelError("NumeroAluno", "Já existe um aluno com este número.");
                vm.Cursos = await GetCursosSelectList();
                return View(vm);
            }

            if (await _context.Alunos.AnyAsync(a => a.NIF == vm.NIF))
            {
                ModelState.AddModelError("NIF", "Já existe um aluno com este NIF.");
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

            await _userManager.AddToRoleAsync(user, "Aluno");
            await _userManager.AddClaimAsync(user, new Claim("MustChangePassword", "true"));

            // Criar entidade Aluno
            var aluno = new Aluno
            {
                Nome = vm.Nome,
                Email = vm.Email,
                Telefone = vm.Telefone,
                NIF = vm.NIF,
                NumeroAluno = vm.NumeroAluno,
                Turma = vm.Turma,
                CursoId = vm.CursoId,
                ApplicationUserId = user.Id,
                Ativo = true
            };

            _context.Alunos.Add(aluno);
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Criar", "Aluno", aluno.Id, $"Aluno '{aluno.Nome}' criado com email {aluno.Email}");

            // Enviar email com credenciais de acesso
            await _emailService.EnviarAsync(vm.Email,
                "SGEEP — Conta Criada",
                EmailTemplates.Envolver(
                    $"<p>Caro(a) <strong>{vm.Nome}</strong>,</p>" +
                    $"<p>Foi criada uma conta no SGEEP para si.</p>" +
                    $"<table style=\"margin:16px 0;border-radius:6px;background:#f8fafc;border:1px solid #e2e8f0;padding:16px 20px;border-collapse:collapse;\">" +
                    $"<tr><td style=\"padding:4px 12px 4px 0;\"><strong>Email:</strong></td><td>{vm.Email}</td></tr>" +
                    $"<tr><td style=\"padding:4px 12px 4px 0;\"><strong>Password tempor&aacute;ria:</strong></td><td><code style=\"background:#f1f5f9;padding:2px 8px;border-radius:4px;font-size:14px;\">{passwordTemporaria}</code></td></tr>" +
                    $"</table>" +
                    $"<p style=\"color:#b45309;background:#fef3c7;border:1px solid #fde68a;border-radius:6px;padding:12px 16px;\">&#9888; Dever&aacute; alterar a password no primeiro acesso.</p>" +
                    $"<p style=\"margin-top:24px;\">Cumprimentos,<br/><strong>SGEEP</strong></p>"));

            TempData["Sucesso"] = $"Aluno {aluno.Nome} criado! Login: {vm.Email} | Password temporária: {passwordTemporaria}";
            return RedirectToAction(nameof(Index));
        }

        // GET: Alunos/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id)
        {
            var aluno = await _context.Alunos.FindAsync(id);
            if (aluno == null) return NotFound();

            return View(new AlunoViewModel
            {
                Id = aluno.Id,
                Nome = aluno.Nome,
                Email = aluno.Email,
                Telefone = aluno.Telefone,
                NIF = aluno.NIF,
                NumeroAluno = aluno.NumeroAluno,
                Turma = aluno.Turma,
                CursoId = aluno.CursoId,
                Ativo = aluno.Ativo,
                Cursos = await GetCursosSelectList()
            });
        }

        // POST: Alunos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, AlunoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Cursos = await GetCursosSelectList();
                return View(vm);
            }

            var aluno = await _context.Alunos.FindAsync(id);
            if (aluno == null) return NotFound();

            if (await _context.Alunos.AnyAsync(a => a.NumeroAluno == vm.NumeroAluno && a.Id != id))
            {
                ModelState.AddModelError("NumeroAluno", "Já existe um aluno com este número.");
                vm.Cursos = await GetCursosSelectList();
                return View(vm);
            }

            if (await _context.Alunos.AnyAsync(a => a.NIF == vm.NIF && a.Id != id))
            {
                ModelState.AddModelError("NIF", "Já existe um aluno com este NIF.");
                vm.Cursos = await GetCursosSelectList();
                return View(vm);
            }

            aluno.Nome = vm.Nome;
            aluno.Email = vm.Email;
            aluno.Telefone = vm.Telefone;
            aluno.NIF = vm.NIF;
            aluno.NumeroAluno = vm.NumeroAluno;
            aluno.Turma = vm.Turma;
            aluno.CursoId = vm.CursoId;
            aluno.Ativo = vm.Ativo;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = $"Aluno {aluno.Nome} atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Alunos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var aluno = await _context.Alunos
                .Include(a => a.Estagios)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (aluno == null) return NotFound();

            if (aluno.Estagios.Any(e => e.Estado == SGEEP.Core.Enums.EstadoEstagio.Ativo))
            {
                TempData["Erro"] = $"Não é possível desativar o aluno {aluno.Nome} porque tem um estágio ativo.";
                return RedirectToAction(nameof(Index));
            }

            // Bloquear conta Identity
            if (aluno.ApplicationUserId != null)
            {
                var user = await _userManager.FindByIdAsync(aluno.ApplicationUserId);
                if (user != null)
                {
                    user.LockoutEnabled = true;
                    user.LockoutEnd = DateTimeOffset.MaxValue;
                    await _userManager.UpdateAsync(user);
                }
            }

            aluno.Ativo = false;
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Desativar", "Aluno", aluno.Id, $"Aluno '{aluno.Nome}' desativado");

            TempData["Sucesso"] = $"Aluno {aluno.Nome} desativado com sucesso!";
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

            // Shuffle
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
