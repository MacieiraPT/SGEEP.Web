using System.ComponentModel.DataAnnotations;

namespace SGEEP.Web.Models.ViewModels
{
    public class ImportViewModel
    {
        [Required(ErrorMessage = "O ficheiro é obrigatório")]
        [Display(Name = "Ficheiro (XLSX ou CSV)")]
        public IFormFile? Ficheiro { get; set; }

        [Required(ErrorMessage = "O tipo de importação é obrigatório")]
        [Display(Name = "Tipo de Importação")]
        public string TipoImportacao { get; set; } = string.Empty;
    }

    public class ImportResultadoViewModel
    {
        public string TipoImportacao { get; set; } = string.Empty;
        public int TotalLinhas { get; set; }
        public int Sucesso { get; set; }
        public int Erros { get; set; }
        public List<ImportLinhaResultado> Resultados { get; set; } = new();
    }

    public class ImportLinhaResultado
    {
        public int Linha { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Importado { get; set; }
        public string? Mensagem { get; set; }
    }
}
