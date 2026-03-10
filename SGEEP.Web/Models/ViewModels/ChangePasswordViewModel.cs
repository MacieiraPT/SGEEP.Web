using System.ComponentModel.DataAnnotations;

namespace SGEEP.Web.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "A password atual é obrigatória")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Atual")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "A nova password é obrigatória")]
        [StringLength(100, ErrorMessage = "A password deve ter pelo menos {2} caracteres.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Nova Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "A confirmação é obrigatória")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nova Password")]
        [Compare("NewPassword", ErrorMessage = "As passwords não coincidem.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
