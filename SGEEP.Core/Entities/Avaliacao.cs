using System.ComponentModel.DataAnnotations;

namespace SGEEP.Core.Entities
{
    public class Avaliacao
    {
        public int Id { get; set; }

        [Range(0, 20)]
        public double NotaFinal { get; set; }

        [Range(0, 20)]
        public double NotaAssiduidade { get; set; }

        [Range(0, 20)]
        public double NotaDesempenho { get; set; }

        [Range(0, 20)]
        public double NotaRelatorio { get; set; }

        [MaxLength(1000)]
        public string? Comentarios { get; set; }

        public DateTime DataAvaliacao { get; set; } = DateTime.UtcNow;

        // Relações
        public int EstagioId { get; set; }
        public Estagio Estagio { get; set; } = null!;
    }
}