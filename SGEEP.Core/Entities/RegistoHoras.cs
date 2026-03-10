using System.ComponentModel.DataAnnotations;
using SGEEP.Core.Enums;

namespace SGEEP.Core.Entities
{
    public class RegistoHoras
    {
        public int Id { get; set; }

        public DateOnly Data { get; set; }
        public TimeOnly HoraEntrada { get; set; }
        public TimeOnly HoraSaida { get; set; }
        public TimeOnly? HoraPausa { get; set; }

        public double TotalHoras =>
            (HoraSaida.ToTimeSpan() - HoraEntrada.ToTimeSpan()
            - (HoraPausa?.ToTimeSpan() ?? TimeSpan.Zero)).TotalHours;

        [MaxLength(500)]
        public string? Observacoes { get; set; }

        public EstadoHoras Estado { get; set; } = EstadoHoras.Pendente;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataValidacao { get; set; }

        // Relações
        public int EstagioId { get; set; }
        public Estagio Estagio { get; set; } = null!;
    }
}