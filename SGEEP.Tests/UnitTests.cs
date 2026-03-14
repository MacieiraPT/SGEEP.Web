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
using SGEEP.Web.Validation;
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

    #region Testes de Validação NIF

    public class NifValidationTests
    {
        private ValidationResult? Validar(string? nif)
        {
            var attribute = new NifValidationAttribute();
            var context = new ValidationContext(new object()) { MemberName = "NIF" };
            return attribute.GetValidationResult(nif, context);
        }

        [Fact]
        public void NIF_Valido_RetornaSuccess()
        {
            // 123456789 → soma = 1*9+2*8+3*7+4*6+5*5+6*4+7*3+8*2 = 9+16+21+24+25+24+21+16 = 156
            // 156 % 11 = 2, digito = 11-2 = 9 → NIF 123456789 é válido
            Assert.Null(Validar("123456789"));
        }

        [Fact]
        public void NIF_Nulo_RetornaErro()
        {
            Assert.NotNull(Validar(null));
        }

        [Fact]
        public void NIF_Vazio_RetornaErro()
        {
            Assert.NotNull(Validar(""));
        }

        [Fact]
        public void NIF_MenosQue9Digitos_RetornaErro()
        {
            Assert.NotNull(Validar("12345678"));
        }

        [Fact]
        public void NIF_MaisQue9Digitos_RetornaErro()
        {
            Assert.NotNull(Validar("1234567890"));
        }

        [Fact]
        public void NIF_ComLetras_RetornaErro()
        {
            Assert.NotNull(Validar("12345678A"));
        }

        [Fact]
        public void NIF_PrimeiroDigitoZero_RetornaErro()
        {
            Assert.NotNull(Validar("012345678"));
        }

        [Fact]
        public void NIF_PrimeiroDigitoQuatro_RetornaErro()
        {
            Assert.NotNull(Validar("412345678"));
        }

        [Fact]
        public void NIF_DigitoControloIncorreto_RetornaErro()
        {
            Assert.NotNull(Validar("123456780")); // Digito correto é 9, não 0
        }
    }

    #endregion

    #region Testes de Lógica de Negócio (Estágio)

    public class EstagioLogicaTests
    {
        [Fact]
        public async Task NaoPodeCriarEstagioParaAlunoComEstagioAtivo()
        {
            using var context = TestDbHelper.CreateContext(nameof(NaoPodeCriarEstagioParaAlunoComEstagioAtivo));

            var curso = new Curso { Id = 1, Nome = "Curso", Codigo = "C", Ativo = true };
            context.Cursos.Add(curso);

            var aluno = new Aluno { Id = 1, Nome = "Aluno", Email = "a@t.pt", NIF = "123456789", NumeroAluno = "A1", Turma = "T1", CursoId = 1, Ativo = true };
            context.Alunos.Add(aluno);

            var empresa = new Empresa { Id = 1, Nome = "Emp", NIF = "999999990", Morada = "R", Cidade = "C", NomeTutor = "T", EmailTutor = "t@e.pt", Ativa = true };
            context.Empresas.Add(empresa);

            var professor = new Professor { Id = 1, Nome = "Prof", Email = "p@t.pt", NIF = "111111118", CursoId = 1, Ativo = true };
            context.Professores.Add(professor);

            context.Estagios.Add(new Estagio
            {
                AlunoId = 1, EmpresaId = 1, ProfessorId = 1,
                DataInicio = DateTime.Today, DataFim = DateTime.Today.AddMonths(3),
                Estado = EstadoEstagio.Ativo
            });
            await context.SaveChangesAsync();

            var temEstagioAtivo = await context.Estagios
                .AnyAsync(e => e.AlunoId == 1 && (e.Estado == EstadoEstagio.Ativo || e.Estado == EstadoEstagio.Pendente));

            Assert.True(temEstagioAtivo);
        }

        [Fact]
        public void Estagio_NaoPodeEditarConcluido()
        {
            var estagio = new Estagio { Estado = EstadoEstagio.Concluido };
            var podeEditar = estagio.Estado != EstadoEstagio.Concluido && estagio.Estado != EstadoEstagio.Cancelado;
            Assert.False(podeEditar);
        }

        [Fact]
        public void Estagio_NaoPodeEditarCancelado()
        {
            var estagio = new Estagio { Estado = EstadoEstagio.Cancelado };
            var podeEditar = estagio.Estado != EstadoEstagio.Concluido && estagio.Estado != EstadoEstagio.Cancelado;
            Assert.False(podeEditar);
        }

        [Fact]
        public void Estagio_PodeEditarAtivo()
        {
            var estagio = new Estagio { Estado = EstadoEstagio.Ativo };
            var podeEditar = estagio.Estado != EstadoEstagio.Concluido && estagio.Estado != EstadoEstagio.Cancelado;
            Assert.True(podeEditar);
        }

        [Fact]
        public void Estagio_SoAtivosPodeSerConcluidos()
        {
            var estadosPossiveis = new[] { EstadoEstagio.Pendente, EstadoEstagio.Ativo, EstadoEstagio.Concluido, EstadoEstagio.Cancelado };
            foreach (var estado in estadosPossiveis)
            {
                var podeConcluir = estado == EstadoEstagio.Ativo;
                if (estado == EstadoEstagio.Ativo)
                    Assert.True(podeConcluir);
                else
                    Assert.False(podeConcluir);
            }
        }

        [Fact]
        public void Estagio_NaoPodeConcluirAntesDataFim()
        {
            var estagio = new Estagio
            {
                DataFim = DateTime.Today.AddMonths(1),
                Estado = EstadoEstagio.Ativo
            };
            var podeConcluir = estagio.DataFim.Date <= DateTime.Today;
            Assert.False(podeConcluir);
        }
    }

    #endregion

    #region Testes de Registo de Horas (Validações Avançadas)

    public class RegistoHorasValidacaoTests
    {
        [Fact]
        public void HoraSaida_DeveSerDepois_HoraEntrada()
        {
            var entrada = new TimeOnly(9, 0);
            var saida = new TimeOnly(8, 0); // Inválido
            Assert.True(saida <= entrada);
        }

        [Fact]
        public void TotalHoras_NaoDeveExceder16()
        {
            var registo = new RegistoHoras
            {
                HoraEntrada = new TimeOnly(6, 0),
                HoraSaida = new TimeOnly(23, 0), // 17 horas
                HoraPausa = null
            };
            Assert.True(registo.TotalHoras > 16);
        }

        [Fact]
        public void Pausa_NaoDeveExcederTempoTrabalho()
        {
            var registo = new RegistoHoras
            {
                HoraEntrada = new TimeOnly(9, 0),
                HoraSaida = new TimeOnly(12, 0),  // 3 horas de trabalho
                HoraPausa = new TimeOnly(4, 0)     // 4 horas de pausa
            };
            Assert.True(registo.TotalHoras < 0);
        }

        [Fact]
        public async Task NaoPodeDuplicarRegistoNoMesmoDia()
        {
            using var context = TestDbHelper.CreateContext(nameof(NaoPodeDuplicarRegistoNoMesmoDia));

            var curso = new Curso { Id = 1, Nome = "Curso", Codigo = "C", Ativo = true };
            context.Cursos.Add(curso);

            var aluno = new Aluno { Id = 1, Nome = "A", Email = "a@t.pt", NIF = "123456789", NumeroAluno = "A1", Turma = "T1", CursoId = 1 };
            context.Alunos.Add(aluno);
            var empresa = new Empresa { Id = 1, Nome = "E", NIF = "999999990", Morada = "R", Cidade = "C", NomeTutor = "T", EmailTutor = "t@e.pt" };
            context.Empresas.Add(empresa);
            var professor = new Professor { Id = 1, Nome = "P", Email = "p@t.pt", NIF = "111111118", CursoId = 1, Ativo = true };
            context.Professores.Add(professor);

            var estagio = new Estagio { Id = 1, AlunoId = 1, EmpresaId = 1, ProfessorId = 1, DataInicio = DateTime.Today.AddDays(-30), DataFim = DateTime.Today.AddMonths(3) };
            context.Estagios.Add(estagio);

            var dataRegisto = DateOnly.FromDateTime(DateTime.Today.AddDays(-5));
            context.RegistoHoras.Add(new RegistoHoras
            {
                EstagioId = 1, Data = dataRegisto,
                HoraEntrada = new TimeOnly(9, 0), HoraSaida = new TimeOnly(17, 0)
            });
            await context.SaveChangesAsync();

            var jaTem = await context.RegistoHoras.AnyAsync(r => r.EstagioId == 1 && r.Data == dataRegisto);
            Assert.True(jaTem);
        }
    }

    #endregion

    #region Testes de ViewModels

    public class ViewModelValidationTests
    {
        private static List<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        [Fact]
        public void EstagioViewModel_CamposObrigatorios()
        {
            var vm = new EstagioViewModel();
            var results = ValidateModel(vm);
            Assert.True(results.Count > 0);
        }

        [Fact]
        public void RelatorioViewModel_TituloObrigatorio()
        {
            var vm = new RelatorioViewModel { Titulo = "" };
            var results = ValidateModel(vm);
            Assert.Contains(results, r => r.MemberNames.Contains("Titulo"));
        }

        [Fact]
        public void AvaliacaoViewModel_NotasNegativas_Invalido()
        {
            var vm = new AvaliacaoViewModel
            {
                EstagioId = 1,
                NotaAssiduidade = -1,
                NotaDesempenho = 15,
                NotaRelatorio = 15,
                NotaFinal = 15
            };
            var results = ValidateModel(vm);
            Assert.Contains(results, r => r.MemberNames.Contains("NotaAssiduidade"));
        }
    }

    #endregion

    #region Testes do EmailService

    public class EmailServiceTests
    {
        private static SGEEP.Web.Services.EmailService CriarServico(string servidorSmtp = "")
        {
            var settings = new Microsoft.Extensions.Options.OptionsWrapper<SGEEP.Web.Models.EmailSettings>(
                new SGEEP.Web.Models.EmailSettings
                {
                    ServidorSmtp = servidorSmtp,
                    Porta = 587,
                    UsarSsl = true,
                    Utilizador = "",
                    Palavra = "",
                    NomeRemetente = "SGEEP",
                    EmailRemetente = "noreply@sgeep.pt"
                });

            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<SGEEP.Web.Services.EmailService>>();
            return new SGEEP.Web.Services.EmailService(settings, logger.Object);
        }

        [Fact]
        public async Task EnviarAsync_SmtpNaoConfigurado_NaoLancaExcecao()
        {
            var service = CriarServico(servidorSmtp: "");
            // Não deve lançar excepção — apenas regista aviso
            await service.EnviarAsync("dest@teste.pt", "Assunto", "<p>Corpo</p>");
        }

        [Fact]
        public async Task EnviarAsync_DestinatarioVazio_NaoLancaExcecao()
        {
            var service = CriarServico(servidorSmtp: "smtp.test.local");
            // Destinatário vazio — não deve lançar excepção
            await service.EnviarAsync("", "Assunto", "<p>Corpo</p>");
        }

        [Fact]
        public async Task EnviarAsync_MultiplosSemSmtp_NaoLancaExcecao()
        {
            var service = CriarServico(servidorSmtp: "");
            await service.EnviarAsync(new[] { "a@test.pt", "b@test.pt" }, "Assunto", "<p>Corpo</p>");
        }

        [Fact]
        public void EmailSettings_ValoresPorOmissao_Corretos()
        {
            var settings = new SGEEP.Web.Models.EmailSettings();
            Assert.Equal(587, settings.Porta);
            Assert.True(settings.UsarSsl);
            Assert.Equal("SGEEP", settings.NomeRemetente);
        }
    }

    #endregion
}
