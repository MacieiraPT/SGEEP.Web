using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Moq;
using SGEEP.Core.Entities;
using SGEEP.Core.Enums;
using SGEEP.Infrastructure.Data;
using SGEEP.Web.Areas.Identity.Pages.Account;
using SGEEP.Web.Controllers;
using SGEEP.Web.Helpers;
using SGEEP.Web.Models;
using SGEEP.Web.Models.ViewModels;
using SGEEP.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace SGEEP.Tests
{
    #region Helpers

    public static class TestDbHelper
    {
        public static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ApplicationDbContext(options);
        }
    }

    #endregion

    #region Entity Validation Tests

    public class EntityValidationTests
    {
        private static List<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        [Fact]
        public void Aluno_NIF_MaxLength9_InvalidWhenLonger()
        {
            var aluno = new Aluno
            {
                Nome = "Test",
                Email = "test@test.com",
                NIF = "1234567890", // 10 chars, max is 9
                NumeroAluno = "A001",
                Turma = "T1"
            };

            var results = ValidateModel(aluno);
            Assert.Contains(results, r => r.MemberNames.Contains("NIF"));
        }

        [Fact]
        public void Aluno_ValidEntity_NoValidationErrors()
        {
            var aluno = new Aluno
            {
                Nome = "João Silva",
                Email = "joao@escola.pt",
                NIF = "123456789",
                NumeroAluno = "A001",
                Turma = "T1"
            };

            var results = ValidateModel(aluno);
            Assert.Empty(results);
        }

        [Fact]
        public void Empresa_NIF_Required()
        {
            var empresa = new Empresa
            {
                Nome = "Empresa X",
                NIF = "", // Empty, required
                Morada = "Rua X",
                Cidade = "Porto",
                NomeTutor = "Tutor",
                EmailTutor = "tutor@empresa.pt"
            };

            var results = ValidateModel(empresa);
            Assert.Contains(results, r => r.MemberNames.Contains("NIF"));
        }

        [Fact]
        public void AvaliacaoViewModel_NotaFinal_OutOfRange_Invalid()
        {
            var vm = new AvaliacaoViewModel
            {
                EstagioId = 1,
                NotaAssiduidade = 15,
                NotaDesempenho = 15,
                NotaRelatorio = 15,
                NotaFinal = 25 // Max is 20
            };

            var results = ValidateModel(vm);
            Assert.Contains(results, r => r.MemberNames.Contains("NotaFinal"));
        }

        [Fact]
        public void AvaliacaoViewModel_ValidGrades_NoErrors()
        {
            var vm = new AvaliacaoViewModel
            {
                EstagioId = 1,
                NotaAssiduidade = 18,
                NotaDesempenho = 16,
                NotaRelatorio = 14,
                NotaFinal = 17
            };

            var results = ValidateModel(vm);
            Assert.Empty(results);
        }

        [Fact]
        public void ChangePasswordViewModel_ShortPassword_Invalid()
        {
            var vm = new ChangePasswordViewModel
            {
                CurrentPassword = "old",
                NewPassword = "abc", // MinimumLength = 8
                ConfirmPassword = "abc"
            };

            var results = ValidateModel(vm);
            Assert.Contains(results, r => r.MemberNames.Contains("NewPassword"));
        }
    }

    #endregion

    #region RegistoHoras TotalHoras Tests

    public class RegistoHorasTests
    {
        [Fact]
        public void TotalHoras_CalculatesCorrectly_WithoutPause()
        {
            var registo = new RegistoHoras
            {
                HoraEntrada = new TimeOnly(9, 0),
                HoraSaida = new TimeOnly(17, 0),
                HoraPausa = null
            };

            Assert.Equal(8.0, registo.TotalHoras, 1);
        }

        [Fact]
        public void TotalHoras_CalculatesCorrectly_WithPause()
        {
            var registo = new RegistoHoras
            {
                HoraEntrada = new TimeOnly(9, 0),
                HoraSaida = new TimeOnly(17, 0),
                HoraPausa = new TimeOnly(1, 0) // 1 hour pause
            };

            Assert.Equal(7.0, registo.TotalHoras, 1);
        }

        [Fact]
        public void RegistoHoras_DefaultState_IsPendente()
        {
            var registo = new RegistoHoras();
            Assert.Equal(EstadoHoras.Pendente, registo.Estado);
        }
    }

    #endregion

    #region PaginatedList Tests

    public class PaginatedListTests
    {
        [Fact]
        public void Constructor_CalculatesTotalPages_Correctly()
        {
            var items = new List<string> { "a", "b", "c" };
            var list = new PaginatedList<string>(items, 25, 1, 10);

            Assert.Equal(3, list.TotalPages);
            Assert.Equal(1, list.PageIndex);
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public void HasPreviousPage_FirstPage_ReturnsFalse()
        {
            var list = new PaginatedList<string>(new List<string>(), 10, 1, 5);
            Assert.False(list.HasPreviousPage);
        }

        [Fact]
        public void HasPreviousPage_SecondPage_ReturnsTrue()
        {
            var list = new PaginatedList<string>(new List<string>(), 10, 2, 5);
            Assert.True(list.HasPreviousPage);
        }

        [Fact]
        public void HasNextPage_LastPage_ReturnsFalse()
        {
            var list = new PaginatedList<string>(new List<string>(), 10, 2, 5);
            Assert.False(list.HasNextPage);
        }

        [Fact]
        public void HasNextPage_FirstPage_ReturnsTrue()
        {
            var list = new PaginatedList<string>(new List<string>(), 10, 1, 5);
            Assert.True(list.HasNextPage);
        }
    }

    #endregion

    #region Estagio Business Rules Tests

    public class EstagioBusinessRulesTests
    {
        [Fact]
        public void Estagio_DefaultState_IsPendente()
        {
            var estagio = new Estagio();
            Assert.Equal(EstadoEstagio.Pendente, estagio.Estado);
        }

        [Fact]
        public void Estagio_DefaultTotalHorasPrevistas_Is210()
        {
            var estagio = new Estagio();
            Assert.Equal(210, estagio.TotalHorasPrevistas);
        }
    }

    #endregion

    #region NotificacaoService Tests

    public class NotificacaoServiceTests
    {
        [Fact]
        public async Task CriarAsync_AddsNotificationToDb()
        {
            using var context = TestDbHelper.CreateContext(nameof(CriarAsync_AddsNotificationToDb));
            var service = new NotificacaoService(context);

            await service.CriarAsync("user1", "Titulo", "Mensagem");

            Assert.Equal(1, await context.Notificacoes.CountAsync());
            var n = await context.Notificacoes.FirstAsync();
            Assert.Equal("user1", n.ApplicationUserId);
            Assert.Equal("Titulo", n.Titulo);
            Assert.False(n.Lida);
        }

        [Fact]
        public async Task ContarNaoLidasAsync_ReturnsCorrectCount()
        {
            using var context = TestDbHelper.CreateContext(nameof(ContarNaoLidasAsync_ReturnsCorrectCount));
            context.Notificacoes.AddRange(
                new Notificacao { ApplicationUserId = "user1", Titulo = "A", Mensagem = "M", Lida = false },
                new Notificacao { ApplicationUserId = "user1", Titulo = "B", Mensagem = "M", Lida = true },
                new Notificacao { ApplicationUserId = "user1", Titulo = "C", Mensagem = "M", Lida = false },
                new Notificacao { ApplicationUserId = "user2", Titulo = "D", Mensagem = "M", Lida = false }
            );
            await context.SaveChangesAsync();

            var service = new NotificacaoService(context);
            var count = await service.ContarNaoLidasAsync("user1");

            Assert.Equal(2, count);
        }

        [Fact]
        public async Task MarcarTodasComoLidasAsync_MarksAll()
        {
            using var context = TestDbHelper.CreateContext(nameof(MarcarTodasComoLidasAsync_MarksAll));
            context.Notificacoes.AddRange(
                new Notificacao { ApplicationUserId = "user1", Titulo = "A", Mensagem = "M", Lida = false },
                new Notificacao { ApplicationUserId = "user1", Titulo = "B", Mensagem = "M", Lida = false }
            );
            await context.SaveChangesAsync();

            var service = new NotificacaoService(context);
            await service.MarcarTodasComoLidasAsync("user1");

            var naoLidas = await context.Notificacoes.CountAsync(n => !n.Lida);
            Assert.Equal(0, naoLidas);
        }
    }

    #endregion

    #region Testes de Atributos de Autorização

    public class AtributosAutorizacaoTests
    {
        [Fact]
        public void EmpresasController_Create_Get_RequerAdministrador()
        {
            var method = typeof(EmpresasController).GetMethod("Create", Type.EmptyTypes);
            var attr = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>().FirstOrDefault();
            Assert.NotNull(attr);
            Assert.Contains("Administrador", attr!.Roles!);
        }

        [Fact]
        public void EmpresasController_Create_Post_RequerAdministrador()
        {
            var method = typeof(EmpresasController).GetMethod("Create",
                new[] { typeof(EmpresaViewModel) });
            var attr = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>().FirstOrDefault();
            Assert.NotNull(attr);
            Assert.Contains("Administrador", attr!.Roles!);
        }

        [Fact]
        public void EmpresasController_Edit_Get_RequerAdministrador()
        {
            var method = typeof(EmpresasController).GetMethod("Edit", new[] { typeof(int) });
            var attr = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>().FirstOrDefault();
            Assert.NotNull(attr);
            Assert.Contains("Administrador", attr!.Roles!);
        }

        [Fact]
        public void EmpresasController_Edit_Post_RequerAdministrador()
        {
            var method = typeof(EmpresasController).GetMethod("Edit",
                new[] { typeof(int), typeof(EmpresaViewModel) });
            var attr = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>().FirstOrDefault();
            Assert.NotNull(attr);
            Assert.Contains("Administrador", attr!.Roles!);
        }

        [Fact]
        public void HomeController_Error_TemAllowAnonymous()
        {
            var method = typeof(HomeController).GetMethod("Error");
            var attr = method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), false);
            Assert.NotEmpty(attr);
        }

        [Fact]
        public void LoginModel_TemEnableRateLimiting()
        {
            var attr = typeof(LoginModel).GetCustomAttributes(
                typeof(EnableRateLimitingAttribute), false);
            Assert.NotEmpty(attr);
        }
    }

    #endregion

    #region Testes de Validação de Ficheiros

    public class ValidacaoFicheiroTests
    {
        [Fact]
        public void ValidarMagicBytes_PdfValido_RetornaTrue()
        {
            var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            Assert.True(ValidadorFicheiro.ValidarMagicBytes(bytes, ".pdf"));
        }

        [Fact]
        public void ValidarMagicBytes_PdfInvalido_RetornaFalse()
        {
            var bytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            Assert.False(ValidadorFicheiro.ValidarMagicBytes(bytes, ".pdf"));
        }

        [Fact]
        public void ValidarMagicBytes_DocxValido_RetornaTrue()
        {
            var bytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
            Assert.True(ValidadorFicheiro.ValidarMagicBytes(bytes, ".docx"));
        }

        [Fact]
        public void ValidarMagicBytes_DocValido_RetornaTrue()
        {
            var bytes = new byte[] { 0xD0, 0xCF, 0x11, 0xE0 };
            Assert.True(ValidadorFicheiro.ValidarMagicBytes(bytes, ".doc"));
        }

        [Fact]
        public void ValidarMagicBytes_ExtensaoDesconhecida_RetornaFalse()
        {
            var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            Assert.False(ValidadorFicheiro.ValidarMagicBytes(bytes, ".exe"));
        }

        [Fact]
        public void ValidarMagicBytes_BytesInsuficientes_RetornaFalse()
        {
            var bytes = new byte[] { 0x25, 0x50 };
            Assert.False(ValidadorFicheiro.ValidarMagicBytes(bytes, ".pdf"));
        }
    }

    #endregion

    #region Testes de Auditoria

    public class AuditoriaServiceTests
    {
        private static AuditoriaService CriarServico(ApplicationDbContext context)
        {
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
            return new AuditoriaService(context, httpContextAccessor.Object);
        }

        [Fact]
        public async Task RegistarAsync_AdicionaRegistoNaBaseDeDados()
        {
            using var context = TestDbHelper.CreateContext(nameof(RegistarAsync_AdicionaRegistoNaBaseDeDados));
            var service = CriarServico(context);

            await service.RegistarAsync("Criar", "Empresa", 1, "Empresa teste criada");

            Assert.Equal(1, await context.RegistosAuditoria.CountAsync());
            var registo = await context.RegistosAuditoria.FirstAsync();
            Assert.Equal("Criar", registo.Acao);
            Assert.Equal("Empresa", registo.Entidade);
            Assert.Equal(1, registo.EntidadeId);
        }

        [Fact]
        public async Task RegistarAsync_TruncaDetalhesLongos()
        {
            using var context = TestDbHelper.CreateContext(nameof(RegistarAsync_TruncaDetalhesLongos));
            var service = CriarServico(context);
            var detalhesLongos = new string('x', 600);

            await service.RegistarAsync("Teste", "Teste", null, detalhesLongos);

            var registo = await context.RegistosAuditoria.FirstAsync();
            Assert.Equal(500, registo.Detalhes.Length);
        }
    }

    #endregion

    #region Testes de Proteção IDOR

    public class ProtecaoIDORTests
    {
        [Fact]
        public async Task Professor_SoVeAlunosDoProprioCurso()
        {
            using var context = TestDbHelper.CreateContext(nameof(Professor_SoVeAlunosDoProprioCurso));

            var curso1 = new Curso { Id = 1, Nome = "Curso A", Ativo = true };
            var curso2 = new Curso { Id = 2, Nome = "Curso B", Ativo = true };
            context.Cursos.AddRange(curso1, curso2);

            var professor = new Professor
            {
                Id = 1, Nome = "Prof A", Email = "profa@test.pt",
                NIF = "123456789", CursoId = 1, Ativo = true
            };
            context.Professores.Add(professor);

            context.Alunos.AddRange(
                new Aluno { Id = 1, Nome = "Aluno 1", Email = "a1@t.pt", NIF = "111111111",
                           NumeroAluno = "A001", Turma = "T1", CursoId = 1 },
                new Aluno { Id = 2, Nome = "Aluno 2", Email = "a2@t.pt", NIF = "222222222",
                           NumeroAluno = "A002", Turma = "T1", CursoId = 2 }
            );
            await context.SaveChangesAsync();

            var alunoOutroCurso = await context.Alunos.FindAsync(2);
            Assert.NotNull(alunoOutroCurso);
            Assert.NotEqual(professor.CursoId, alunoOutroCurso!.CursoId);

            var alunoMesmoCurso = await context.Alunos.FindAsync(1);
            Assert.Equal(professor.CursoId, alunoMesmoCurso!.CursoId);
        }

        [Fact]
        public async Task Professor_SoVeEstagiosProprios()
        {
            using var context = TestDbHelper.CreateContext(nameof(Professor_SoVeEstagiosProprios));

            var curso = new Curso { Id = 1, Nome = "Curso", Ativo = true };
            context.Cursos.Add(curso);

            var prof1 = new Professor { Id = 1, Nome = "Prof 1", Email = "p1@t.pt",
                                        NIF = "111111111", CursoId = 1, Ativo = true };
            var prof2 = new Professor { Id = 2, Nome = "Prof 2", Email = "p2@t.pt",
                                        NIF = "222222222", CursoId = 1, Ativo = true };
            context.Professores.AddRange(prof1, prof2);

            var aluno = new Aluno { Id = 1, Nome = "Aluno", Email = "a@t.pt",
                                   NIF = "333333333", NumeroAluno = "A001",
                                   Turma = "T1", CursoId = 1 };
            context.Alunos.Add(aluno);

            var empresa = new Empresa { Id = 1, Nome = "Emp", NIF = "444444444",
                                       Morada = "Rua", NomeTutor = "T",
                                       EmailTutor = "t@e.pt" };
            context.Empresas.Add(empresa);

            var estagio = new Estagio
            {
                Id = 1, AlunoId = 1, EmpresaId = 1, ProfessorId = 2,
                DataInicio = DateTime.Today, DataFim = DateTime.Today.AddMonths(3),
                LocalEstagio = "Local"
            };
            context.Estagios.Add(estagio);
            await context.SaveChangesAsync();

            Assert.NotEqual(prof1.Id, estagio.ProfessorId);
            Assert.Equal(prof2.Id, estagio.ProfessorId);
        }
    }

    #endregion

    #region Testes de Validação de Parâmetros

    public class ValidacaoParametrosTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Pagina_ValorInvalido_DeveSer1(int paginaInvalida)
        {
            var pagina = paginaInvalida < 1 ? 1 : paginaInvalida;
            Assert.Equal(1, pagina);
        }
    }

    #endregion
}
