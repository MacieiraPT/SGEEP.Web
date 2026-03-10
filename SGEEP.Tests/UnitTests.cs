using Microsoft.EntityFrameworkCore;
using SGEEP.Core.Entities;
using SGEEP.Core.Enums;
using SGEEP.Infrastructure.Data;
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
}
