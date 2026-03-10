using System.ComponentModel.DataAnnotations;

namespace SGEEP.Core.Entities
{
    public class Empresa
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required, MaxLength(9)]
        public string NIF { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Morada { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Cidade { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Setor { get; set; }

        [Required, MaxLength(100)]
        public string NomeTutor { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string EmailTutor { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? TelefoneTutor { get; set; }

        public bool Ativa { get; set; } = true;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Identity
        public string? ApplicationUserId { get; set; }

        // Relações
        public ICollection<Estagio> Estagios { get; set; } = new List<Estagio>();
    }
}