using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SGEEP.Core.Entities;
using SGEEP.Core.Enums;
using SGEEP.Infrastructure.Data;
using SGEEP.Web.Models;
using SGEEP.Web.Models.ViewModels;
using SGEEP.Web.Services;

namespace SGEEP.Web.Controllers
{
    [Authorize(Roles = "Administrador,Professor")]
    public class EstagiosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificacaoService _notificacaoService;
        private readonly AuditoriaService _auditoria;

        public EstagiosController(ApplicationDbContext context, NotificacaoService notificacaoService, AuditoriaService auditoria)
        {
            _context = context;
            _notificacaoService = notificacaoService;
            _auditoria = auditoria;
        }

        // GET: Estagios
        public async Task<IActionResult> Index(string? pesquisa, EstadoEstagio? estado, int? pagina)
        {
            var query = _context.Estagios
                .Include(e => e.Aluno)
                .Include(e => e.Empresa)
                .Include(e => e.Professor)
                .AsQueryable();

            // Professor só vê os seus estágios
            if (User.IsInRole("Professor"))
            {
                var userEmail = User.Identity!.Name;
                var professor = await _context.Professores
                    .FirstOrDefaultAsync(p => p.Email == userEmail && p.Ativo);

                if (professor == null)
                {
                    TempData["Erro"] = "Perfil de professor não encontrado.";
                    return View(new PaginatedList<Estagio>(new List<Estagio>(), 0, 1, 15));
                }

                query = query.Where(e => e.ProfessorId == professor.Id);
            }

            if (!string.IsNullOrEmpty(pesquisa))
                query = query.Where(e =>
                    e.Aluno.Nome.Contains(pesquisa) ||
                    e.Empresa.Nome.Contains(pesquisa));

            if (estado.HasValue)
                query = query.Where(e => e.Estado == estado);

            ViewBag.Pesquisa = pesquisa;
            ViewBag.Estado = estado;
            ViewBag.Estados = new SelectList(
                Enum.GetValues<EstadoEstagio>()
                    .Select(e => new { Value = (int)e, Text = e.ToString() }),
                "Value", "Text");

            return View(await PaginatedList<Estagio>.CreateAsync(
                query.OrderByDescending(e => e.DataInicio), pagina ?? 1, 15));
        }

        // GET: Estagios/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var estagio = await _context.Estagios
                .Include(e => e.Aluno).ThenInclude(a => a.Curso)
                .Include(e => e.Empresa)
                .Include(e => e.Professor)
                .Include(e => e.RegistoHoras)
                .Include(e => e.Relatorios)
                .Include(e => e.Avaliacao)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (estagio == null) return NotFound();

            // Professor só pode ver os seus estágios
            if (User.IsInRole("Professor"))
            {
                var userEmail = User.Identity!.Name;
                var professor = await _context.Professores
                    .FirstOrDefaultAsync(p => p.Email == userEmail);

                if (estagio.ProfessorId != professor?.Id)
                    return Forbid();
            }

            return View(estagio);
        }

        // GET: Estagios/Create
        public async Task<IActionResult> Create()
        {
            var vm = new EstagioViewModel
            {
                DataInicio = DateTime.Today,
                DataFim = DateTime.Today.AddMonths(3)
            };

            await PopularDropdowns(vm);
            return View(vm);
        }

        // POST: Estagios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EstagioViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopularDropdowns(vm);
                return View(vm);
            }

            // Validar datas
            if (vm.DataFim <= vm.DataInicio)
            {
                ModelState.AddModelError("DataFim", "A data de fim deve ser posterior à data de início.");
                await PopularDropdowns(vm);
                return View(vm);
            }

            // Verificar se aluno já tem estágio ativo
            var temEstagioAtivo = await _context.Estagios
                .AnyAsync(e => e.AlunoId == vm.AlunoId &&
                    (e.Estado == EstadoEstagio.Ativo || e.Estado == EstadoEstagio.Pendente));

            if (temEstagioAtivo)
            {
                ModelState.AddModelError("AlunoId", "Este aluno já tem um estágio ativo ou pendente.");
                await PopularDropdowns(vm);
                return View(vm);
            }

            var estagio = new Estagio
            {
                AlunoId = vm.AlunoId,
                EmpresaId = vm.EmpresaId,
                ProfessorId = vm.ProfessorId,
                DataInicio = vm.DataInicio,
                DataFim = vm.DataFim,
                LocalEstagio = vm.LocalEstagio,
                TotalHorasPrevistas = vm.TotalHorasPrevistas,
                Observacoes = vm.Observacoes,
                Estado = EstadoEstagio.Pendente
            };

            _context.Estagios.Add(estagio);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Estágio criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Estagios/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var estagio = await _context.Estagios.FindAsync(id);
            if (estagio == null) return NotFound();

            if (!await ProfessorTemAcesso(estagio.ProfessorId))
                return Forbid();

            // Não permitir editar estágios concluídos ou cancelados
            if (estagio.Estado == EstadoEstagio.Concluido ||
                estagio.Estado == EstadoEstagio.Cancelado)
            {
                TempData["Erro"] = "Não é possível editar um estágio concluído ou cancelado.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new EstagioViewModel
            {
                Id = estagio.Id,
                AlunoId = estagio.AlunoId,
                EmpresaId = estagio.EmpresaId,
                ProfessorId = estagio.ProfessorId,
                DataInicio = estagio.DataInicio,
                DataFim = estagio.DataFim,
                LocalEstagio = estagio.LocalEstagio,
                TotalHorasPrevistas = estagio.TotalHorasPrevistas,
                Observacoes = estagio.Observacoes,
                Estado = estagio.Estado
            };

            await PopularDropdowns(vm);
            return View(vm);
        }

        // POST: Estagios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EstagioViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopularDropdowns(vm);
                return View(vm);
            }

            var estagio = await _context.Estagios.FindAsync(id);
            if (estagio == null) return NotFound();

            if (!await ProfessorTemAcesso(estagio.ProfessorId))
                return Forbid();

            if (vm.DataFim <= vm.DataInicio)
            {
                ModelState.AddModelError("DataFim", "A data de fim deve ser posterior à data de início.");
                await PopularDropdowns(vm);
                return View(vm);
            }

            // Não permitir editar estágios concluídos ou cancelados
            if (estagio.Estado == EstadoEstagio.Concluido ||
                estagio.Estado == EstadoEstagio.Cancelado)
            {
                TempData["Erro"] = "Não é possível editar um estágio concluído ou cancelado.";
                return RedirectToAction(nameof(Index));
            }

            // Validar que as novas datas não invalidam registos de horas existentes
            var dataInicioNova = DateOnly.FromDateTime(vm.DataInicio);
            var dataFimNova = DateOnly.FromDateTime(vm.DataFim);
            var registosForaDoPeriodo = await _context.RegistoHoras
                .AnyAsync(r => r.EstagioId == id && (r.Data < dataInicioNova || r.Data > dataFimNova));

            if (registosForaDoPeriodo)
            {
                ModelState.AddModelError("DataInicio", "Existem registos de horas fora do novo período. Ajuste as datas ou remova os registos primeiro.");
                await PopularDropdowns(vm);
                return View(vm);
            }

            estagio.EmpresaId = vm.EmpresaId;
            // Apenas administradores podem reatribuir o professor orientador
            if (User.IsInRole("Administrador"))
                estagio.ProfessorId = vm.ProfessorId;
            estagio.DataInicio = vm.DataInicio;
            estagio.DataFim = vm.DataFim;
            estagio.LocalEstagio = vm.LocalEstagio;
            estagio.TotalHorasPrevistas = vm.TotalHorasPrevistas;
            estagio.Observacoes = vm.Observacoes;
            // Estado não é alterável via edição — usar ações específicas (Ativar/Cancelar/Concluir)

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Estágio atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Estagios/Ativar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ativar(int id)
        {
            var estagio = await _context.Estagios
                .Include(e => e.Aluno)
                .Include(e => e.Empresa)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (estagio == null) return NotFound();

            if (!await ProfessorTemAcesso(estagio.ProfessorId))
                return Forbid();

            if (estagio.Estado != EstadoEstagio.Pendente)
            {
                TempData["Erro"] = "Apenas estágios pendentes podem ser ativados.";
                return RedirectToAction(nameof(Index));
            }

            estagio.Estado = EstadoEstagio.Ativo;
            await _context.SaveChangesAsync();

            // Notificar aluno
            if (!string.IsNullOrEmpty(estagio.Aluno?.ApplicationUserId))
                await _notificacaoService.CriarAsync(estagio.Aluno.ApplicationUserId,
                    "Estágio Ativado", $"O seu estágio na empresa {estagio.Empresa?.Nome} foi ativado.");

            // Notificar empresa
            if (!string.IsNullOrEmpty(estagio.Empresa?.ApplicationUserId))
                await _notificacaoService.CriarAsync(estagio.Empresa.ApplicationUserId,
                    "Novo Estágio Ativo", $"O estágio do aluno {estagio.Aluno?.Nome} foi ativado.");

            TempData["Sucesso"] = "Estágio ativado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Estagios/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var estagio = await _context.Estagios
                .Include(e => e.RegistoHoras)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (estagio == null) return NotFound();

            if (!await ProfessorTemAcesso(estagio.ProfessorId))
                return Forbid();

            if (estagio.Estado == EstadoEstagio.Concluido)
            {
                TempData["Erro"] = "Não é possível cancelar um estágio já concluído.";
                return RedirectToAction(nameof(Index));
            }

            estagio.Estado = EstadoEstagio.Cancelado;
            await _context.SaveChangesAsync();

            await _auditoria.RegistarAsync("Cancelar", "Estagio", estagio.Id, $"Estágio #{estagio.Id} cancelado");

            TempData["Sucesso"] = "Estágio cancelado.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Estagios/Concluir/5
        public async Task<IActionResult> Concluir(int id)
        {
            var estagio = await _context.Estagios
                .Include(e => e.Aluno)
                .Include(e => e.Empresa)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (estagio == null) return NotFound();

            if (!await ProfessorTemAcesso(estagio.ProfessorId))
                return Forbid();

            if (estagio.Estado != EstadoEstagio.Ativo)
            {
                TempData["Erro"] = "Apenas estágios ativos podem ser concluídos.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new AvaliacaoViewModel
            {
                EstagioId = estagio.Id,
                AlunoNome = estagio.Aluno?.Nome,
                EmpresaNome = estagio.Empresa?.Nome
            };

            return View(vm);
        }

        // POST: Estagios/Concluir/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Concluir(int id, AvaliacaoViewModel vm)
        {
            var estagio = await _context.Estagios
                .Include(e => e.Aluno)
                .Include(e => e.Empresa)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (estagio == null) return NotFound();

            if (!await ProfessorTemAcesso(estagio.ProfessorId))
                return Forbid();

            if (estagio.Estado != EstadoEstagio.Ativo)
            {
                TempData["Erro"] = "Apenas estágios ativos podem ser concluídos.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar horas mínimas validadas
            var horasValidadas = await _context.RegistoHoras
                .Where(r => r.EstagioId == id && r.Estado == EstadoHoras.Validado)
                .ToListAsync();
            var totalHorasValidadas = horasValidadas.Sum(r => r.TotalHoras);

            if (totalHorasValidadas < estagio.TotalHorasPrevistas)
            {
                TempData["Erro"] = $"O aluno tem apenas {totalHorasValidadas:F1}h validadas de {estagio.TotalHorasPrevistas}h previstas. Não é possível concluir o estágio sem atingir o mínimo de horas.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Verificar que existe pelo menos um relatório aprovado
            var temRelatorioAprovado = await _context.Relatorios
                .AnyAsync(r => r.EstagioId == id && r.Estado == EstadoRelatorio.Aprovado);

            if (!temRelatorioAprovado)
            {
                TempData["Erro"] = "Não é possível concluir o estágio sem pelo menos um relatório aprovado.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!ModelState.IsValid)
            {
                vm.EstagioId = estagio.Id;
                vm.AlunoNome = estagio.Aluno?.Nome;
                vm.EmpresaNome = estagio.Empresa?.Nome;
                return View(vm);
            }

            // Criar avaliação
            var avaliacao = new Avaliacao
            {
                EstagioId = estagio.Id,
                NotaFinal = vm.NotaFinal,
                NotaAssiduidade = vm.NotaAssiduidade,
                NotaDesempenho = vm.NotaDesempenho,
                NotaRelatorio = vm.NotaRelatorio,
                Comentarios = vm.Comentarios,
                DataAvaliacao = DateTime.UtcNow
            };

            _context.Avaliacoes.Add(avaliacao);
            estagio.Estado = EstadoEstagio.Concluido;
            await _context.SaveChangesAsync();

            // Notificar aluno
            if (!string.IsNullOrEmpty(estagio.Aluno?.ApplicationUserId))
                await _notificacaoService.CriarAsync(estagio.Aluno.ApplicationUserId,
                    "Estágio Concluído", $"O seu estágio foi concluído com nota final de {vm.NotaFinal:F1}.");

            await _auditoria.RegistarAsync("Concluir", "Estagio", estagio.Id, $"Estágio #{estagio.Id} concluído com nota final {vm.NotaFinal:F1}");

            TempData["Sucesso"] = "Estágio concluído e avaliação registada com sucesso!";
            return RedirectToAction(nameof(Details), new { id = estagio.Id });
        }

        private async Task<bool> ProfessorTemAcesso(int professorIdDoEstagio)
        {
            if (!User.IsInRole("Professor")) return true;
            var userEmail = User.Identity!.Name;
            var professor = await _context.Professores
                .FirstOrDefaultAsync(p => p.Email == userEmail);
            return professor?.Id == professorIdDoEstagio;
        }

        private async Task PopularDropdowns(EstagioViewModel vm)
        {
            // Se for professor, só mostra os alunos do seu curso
            IQueryable<Aluno> alunosQuery = _context.Alunos
                .Include(a => a.Curso)
                .Where(a => a.Ativo);

            if (User.IsInRole("Professor"))
            {
                var userEmail = User.Identity!.Name;
                var professor = await _context.Professores
                    .FirstOrDefaultAsync(p => p.Email == userEmail);

                if (professor != null)
                    alunosQuery = alunosQuery.Where(a => a.CursoId == professor.CursoId);
            }

            vm.Alunos = await alunosQuery
                .OrderBy(a => a.Nome)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = $"{a.Nome} — {a.Curso.Nome} ({a.Turma})"
                })
                .ToListAsync();

            vm.Empresas = await _context.Empresas
                .Where(e => e.Ativa)
                .OrderBy(e => e.Nome)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = e.Nome
                })
                .ToListAsync();

            vm.Professores = await _context.Professores
                .Where(p => p.Ativo)
                .Include(p => p.Curso)
                .OrderBy(p => p.Nome)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Nome} — {p.Curso.Nome}"
                })
                .ToListAsync();
        }
    }
}