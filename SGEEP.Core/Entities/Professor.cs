using System.ComponentModel.DataAnnotations;

namespace SGEEP.Core.Entities
{
    public class Professor
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefone { get; set; }

        [Required, MaxLength(9)]
        public string NIF { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Relações
        public string? ApplicationUserId { get; set; }

        public int CursoId { get; set; }
        public Curso Curso { get; set; } = null!;

        public ICollection<Estagio> Estagios { get; set; } = new List<Estagio>();
    }
}