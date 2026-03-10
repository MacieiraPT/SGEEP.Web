using System.ComponentModel.DataAnnotations;
using SGEEP.Web.Validation;

namespace SGEEP.Web.Models.ViewModels
{
    public class EmpresaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        [MaxLength(150)]
        [Display(Name = "Nome da Empresa")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O NIF é obrigatório")]
        [NifValidation]
        [Display(Name = "NIF")]
        public string NIF { get; set; } = string.Empty;

        [Required(ErrorMessage = "A morada é obrigatória")]
        [MaxLength(200)]
        [Display(Name = "Morada")]
        public string Morada { get; set; } = string.Empty;

        [Required(ErrorMessage = "A cidade é obrigatória")]
        [MaxLength(100)]
        [Display(Name = "Cidade")]
        public string Cidade { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Setor de Atividade")]
        public string? Setor { get; set; }

        [Required(ErrorMessage = "O nome do tutor é obrigatório")]
        [MaxLength(100)]
        [Display(Name = "Nome do Tutor")]
        public string NomeTutor { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email do tutor é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Email do Tutor")]
        public string EmailTutor { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Telefone inválido")]
        [Display(Name = "Telefone do Tutor")]
        public string? TelefoneTutor { get; set; }

        [Display(Name = "Ativa")]
        public bool Ativa { get; set; } = true;
    }
}