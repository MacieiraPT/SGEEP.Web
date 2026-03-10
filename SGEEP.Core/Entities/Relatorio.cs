using System.ComponentModel.DataAnnotations;
using SGEEP.Core.Enums;

namespace SGEEP.Core.Entities
{
    public class Relatorio
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        public string? FicheiroPath { get; set; }

        [MaxLength(1000)]
        public string? Descricao { get; set; }

        [MaxLength(1000)]
        public string? ComentarioProfessor { get; set; }

        public EstadoRelatorio Estado { get; set; } = EstadoRelatorio.Rascunho;

        public DateTime DataSubmissao { get; set; } = DateTime.UtcNow;
        public DateTime? DataAvaliacao { get; set; }

        // Relações
        public int EstagioId { get; set; }
        public Estagio Estagio { get; set; } = null!;
    }
}