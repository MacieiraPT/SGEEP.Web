using System.ComponentModel.DataAnnotations;
using SGEEP.Core.Enums;

namespace SGEEP.Web.Models.ViewModels
{
    public class RelatorioViewModel
    {
        public int Id { get; set; }

        public int EstagioId { get; set; }

        [Required(ErrorMessage = "O título é obrigatório")]
        [MaxLength(200)]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Display(Name = "Ficheiro (PDF ou DOCX)")]
        public IFormFile? Ficheiro { get; set; }

        public string? FicheiroPath { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Comentário do Professor")]
        public string? ComentarioProfessor { get; set; }

        public EstadoRelatorio Estado { get; set; } = EstadoRelatorio.Rascunho;
    }
}