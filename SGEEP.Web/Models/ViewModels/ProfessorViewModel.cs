using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using SGEEP.Web.Validation;

namespace SGEEP.Web.Models.ViewModels
{
    public class ProfessorViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        [MaxLength(100)]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Telefone inválido")]
        [Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        [Required(ErrorMessage = "O NIF é obrigatório")]
        [NifValidation]
        [Display(Name = "NIF")]
        public string NIF { get; set; } = string.Empty;

        [Required(ErrorMessage = "A área é obrigatória")]
        [Display(Name = "Área")]
        public int CursoId { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Diretor de Curso")]
        public bool DiretorCurso { get; set; }

        public IEnumerable<SelectListItem> Cursos { get; set; } = new List<SelectListItem>();
    }
}