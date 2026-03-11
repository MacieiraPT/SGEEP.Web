using System.ComponentModel.DataAnnotations;

namespace SGEEP.Core.Entities
{
    public class RegistoAuditoria
    {
        public int Id { get; set; }

        public DateTime DataHora { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(100)]
        public string Acao { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Entidade { get; set; } = string.Empty;

        public int? EntidadeId { get; set; }

        [MaxLength(500)]
        public string Detalhes { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string UtilizadorEmail { get; set; } = string.Empty;

        public string ApplicationUserId { get; set; } = string.Empty;

        [MaxLength(45)]
        public string EnderecoIP { get; set; } = string.Empty;
    }
}
