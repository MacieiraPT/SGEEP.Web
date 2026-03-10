using System.ComponentModel.DataAnnotations;

namespace SGEEP.Web.Models.ViewModels
{
    public class AvaliacaoViewModel
    {
        public int EstagioId { get; set; }

        // Dados do estágio (para contexto na view)
        public string? AlunoNome { get; set; }
        public string? EmpresaNome { get; set; }

        [Required(ErrorMessage = "A nota de assiduidade é obrigatória")]
        [Range(0, 20, ErrorMessage = "A nota deve estar entre 0 e 20")]
        [Display(Name = "Assiduidade")]
        public double NotaAssiduidade { get; set; }

        [Required(ErrorMessage = "A nota de desempenho é obrigatória")]
        [Range(0, 20, ErrorMessage = "A nota deve estar entre 0 e 20")]
        [Display(Name = "Desempenho")]
        public double NotaDesempenho { get; set; }

        [Required(ErrorMessage = "A nota de relatório é obrigatória")]
        [Range(0, 20, ErrorMessage = "A nota deve estar entre 0 e 20")]
        [Display(Name = "Relatório")]
        public double NotaRelatorio { get; set; }

        [Required(ErrorMessage = "A nota final é obrigatória")]
        [Range(0, 20, ErrorMessage = "A nota deve estar entre 0 e 20")]
        [Display(Name = "Nota Final")]
        public double NotaFinal { get; set; }

        [MaxLength(1000, ErrorMessage = "Os comentários não podem exceder 1000 caracteres")]
        [Display(Name = "Comentários")]
        public string? Comentarios { get; set; }
    }
}
