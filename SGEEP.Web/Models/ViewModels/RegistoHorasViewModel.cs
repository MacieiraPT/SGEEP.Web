using System.ComponentModel.DataAnnotations;
using SGEEP.Core.Enums;

namespace SGEEP.Web.Models.ViewModels
{
    public class RegistoHorasViewModel
    {
        public int Id { get; set; }

        public int EstagioId { get; set; }

        [Required(ErrorMessage = "A data é obrigatória")]
        [Display(Name = "Data")]
        [DataType(DataType.Date)]
        public DateOnly Data { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required(ErrorMessage = "A hora de entrada é obrigatória")]
        [Display(Name = "Hora de Entrada")]
        [DataType(DataType.Time)]
        public TimeOnly HoraEntrada { get; set; } = new TimeOnly(9, 0);

        [Required(ErrorMessage = "A hora de saída é obrigatória")]
        [Display(Name = "Hora de Saída")]
        [DataType(DataType.Time)]
        public TimeOnly HoraSaida { get; set; } = new TimeOnly(18, 0);

        [Display(Name = "Pausa (duração)")]
        [DataType(DataType.Time)]
        public TimeOnly? HoraPausa { get; set; } = new TimeOnly(1, 0);

        [Display(Name = "Observações")]
        [MaxLength(500)]
        public string? Observacoes { get; set; }

        public EstadoHoras Estado { get; set; } = EstadoHoras.Pendente;
    }
}