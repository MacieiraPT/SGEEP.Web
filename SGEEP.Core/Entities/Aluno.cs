using System.ComponentModel.DataAnnotations;

namespace SGEEP.Core.Entities
{
    public class Aluno
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

        [Required, MaxLength(20)]
        public string NumeroAluno { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Turma { get; set; } = string.Empty;

        public string? FotoPath { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public bool Ativo { get; set; } = true;

        // Relações
        public string? ApplicationUserId { get; set; }

        public int CursoId { get; set; }
        public Curso Curso { get; set; } = null!;

        public ICollection<Estagio> Estagios { get; set; } = new List<Estagio>();
    }
}