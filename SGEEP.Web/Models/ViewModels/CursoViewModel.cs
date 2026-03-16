using System.ComponentModel.DataAnnotations;

namespace SGEEP.Web.Models.ViewModels
{
    public class CursoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        [MaxLength(150)]
        [Display(Name = "Nome do Curso")]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(20)]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;
    }
}
