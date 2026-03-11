using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGEEP.Infrastructure.Data;
using SGEEP.Web.Models;

namespace SGEEP.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AuditoriaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditoriaController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string pesquisa,
            string acao,
            string entidade,
            int pagina = 1)
        {
            var query = _context.RegistosAuditoria.AsQueryable();

            if (!string.IsNullOrWhiteSpace(pesquisa))
                query = query.Where(r =>
                    r.UtilizadorEmail.Contains(pesquisa) ||
                    r.Detalhes.Contains(pesquisa));

            if (!string.IsNullOrWhiteSpace(acao))
                query = query.Where(r => r.Acao == acao);

            if (!string.IsNullOrWhiteSpace(entidade))
                query = query.Where(r => r.Entidade == entidade);

            query = query.OrderByDescending(r => r.DataHora);

            ViewBag.Pesquisa = pesquisa;
            ViewBag.Acao = acao;
            ViewBag.Entidade = entidade;
            ViewBag.Acoes = await _context.RegistosAuditoria
                .Select(r => r.Acao).Distinct().OrderBy(a => a).ToListAsync();
            ViewBag.Entidades = await _context.RegistosAuditoria
                .Select(r => r.Entidade).Distinct().OrderBy(e => e).ToListAsync();

            var registos = await PaginatedList<SGEEP.Core.Entities.RegistoAuditoria>
                .CreateAsync(query, pagina, 20);

            return View(registos);
        }
    }
}
