using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGEEP.Core.Entities;
using SGEEP.Infrastructure.Data;
using SGEEP.Web.Models;
using SGEEP.Web.Models.ViewModels;
using SGEEP.Web.Services;

namespace SGEEP.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;

        public CursosController(ApplicationDbContext context, AuditoriaService auditoria)
        {
            _context = context;
            _auditoria = auditoria;
        }

        // GET: Cursos
        public async Task<IActionResult> Index(string? pesquisa, int? pagina)
        {
            var query = _context.Cursos.AsQueryable();

            if (!string.IsNullOrEmpty(pesquisa))
                query = query.Where(c =>
                    c.Nome.Contains(pesquisa) ||
                    c.Codigo.Contains(pesquisa));

            ViewBag.Pesquisa = pesquisa;
            return View(await PaginatedList<Curso>.CreateAsync(
                query.OrderBy(c => c.Nome), pagina ?? 1, 15));
        }

        // GET: Cursos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var curso = await _context.Cursos
                .Include(c => c.Alunos)
                .Include(c => c.Professores)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (curso == null) return NotFound();
            return View(curso);
        }

        // GET: Cursos/Create
        public IActionResult Create()
        {
            return View(new CursoViewModel());
        }

        // POST: Cursos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CursoViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            if (await _context.Cursos.AnyAsync(c => c.Codigo == vm.Codigo && !string.IsNullOrEmpty(vm.Codigo)))
            {
                ModelState.AddModelError("Codigo", "Já existe um curso com este código.");
                return View(vm);
            }

            var curso = new Curso
            {
                Nome = vm.Nome,
                Codigo = vm.Codigo,
                Descricao = vm.Descricao,
                Ativo = vm.Ativo
            };

            _context.Cursos.Add(curso);
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Criar", "Curso", curso.Id, $"Curso '{curso.Nome}' ({curso.Codigo}) criado");

            TempData["Sucesso"] = $"Curso {curso.Nome} criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Cursos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null) return NotFound();

            return View(new CursoViewModel
            {
                Id = curso.Id,
                Nome = curso.Nome,
                Codigo = curso.Codigo,
                Descricao = curso.Descricao,
                Ativo = curso.Ativo
            });
        }

        // POST: Cursos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CursoViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null) return NotFound();

            if (await _context.Cursos.AnyAsync(c => c.Codigo == vm.Codigo && c.Id != id && !string.IsNullOrEmpty(vm.Codigo)))
            {
                ModelState.AddModelError("Codigo", "Já existe um curso com este código.");
                return View(vm);
            }

            curso.Nome = vm.Nome;
            curso.Codigo = vm.Codigo;
            curso.Descricao = vm.Descricao;
            curso.Ativo = vm.Ativo;

            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Editar", "Curso", curso.Id, $"Curso '{curso.Nome}' editado");

            TempData["Sucesso"] = $"Curso {curso.Nome} atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Cursos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var curso = await _context.Cursos
                .Include(c => c.Alunos)
                .Include(c => c.Professores)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (curso == null) return NotFound();

            if (curso.Alunos.Any(a => a.Ativo) || curso.Professores.Any(p => p.Ativo))
            {
                TempData["Erro"] = $"Não é possível desativar o curso {curso.Nome} porque tem alunos ou professores ativos.";
                return RedirectToAction(nameof(Index));
            }

            curso.Ativo = false;
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Desativar", "Curso", curso.Id, $"Curso '{curso.Nome}' desativado");

            TempData["Sucesso"] = $"Curso {curso.Nome} desativado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
    }
}
