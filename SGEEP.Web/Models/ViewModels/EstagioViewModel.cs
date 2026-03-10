using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using SGEEP.Core.Enums;

namespace SGEEP.Web.Models.ViewModels
{
    public class EstagioViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O aluno é obrigatório")]
        [Display(Name = "Aluno")]
        public int AlunoId { get; set; }

        [Required(ErrorMessage = "A empresa é obrigatória")]
        [Display(Name = "Empresa")]
        public int EmpresaId { get; set; }

        [Required(ErrorMessage = "O professor é obrigatório")]
        [Display(Name = "Professor Orientador")]
        public int ProfessorId { get; set; }

        [Required(ErrorMessage = "A data de início é obrigatória")]
        [Display(Name = "Data de Início")]
        [DataType(DataType.Date)]
        public DateTime DataInicio { get; set; }

        [Required(ErrorMessage = "A data de fim é obrigatória")]
        [Display(Name = "Data de Fim")]
        [DataType(DataType.Date)]
        public DateTime DataFim { get; set; }

        [Display(Name = "Local de Estágio")]
        [MaxLength(200)]
        public string? LocalEstagio { get; set; }

        [Display(Name = "Total de Horas Previstas")]
        [Range(1, 1000, ErrorMessage = "O total de horas deve ser entre 1 e 1000")]
        public int TotalHorasPrevistas { get; set; } = 210;

        [Display(Name = "Observações")]
        [MaxLength(500)]
        public string? Observacoes { get; set; }

        [Display(Name = "Estado")]
        public EstadoEstagio Estado { get; set; } = EstadoEstagio.Pendente;

        // Para os dropdowns
        public IEnumerable<SelectListItem> Alunos { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Empresas { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Professores { get; set; } = new List<SelectListItem>();
    }
}