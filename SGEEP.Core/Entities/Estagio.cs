using System.ComponentModel.DataAnnotations;
using SGEEP.Core.Enums;

namespace SGEEP.Core.Entities
{
    public class Estagio
    {
        public int Id { get; set; }

        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        [MaxLength(200)]
        public string? LocalEstagio { get; set; }

        [MaxLength(500)]
        public string? Observacoes { get; set; }

        public int TotalHorasPrevistas { get; set; } = 210;

        public EstadoEstagio Estado { get; set; } = EstadoEstagio.Pendente;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Relações
        public int AlunoId { get; set; }
        public Aluno Aluno { get; set; } = null!;

        public int EmpresaId { get; set; }
        public Empresa Empresa { get; set; } = null!;

        public int ProfessorId { get; set; }
        public Professor Professor { get; set; } = null!;

        public ICollection<RegistoHoras> RegistoHoras { get; set; } = new List<RegistoHoras>();
        public ICollection<Relatorio> Relatorios { get; set; } = new List<Relatorio>();
        public Avaliacao? Avaliacao { get; set; }
    }
}