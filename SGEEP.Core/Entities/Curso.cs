using System.ComponentModel.DataAnnotations;

namespace SGEEP.Core.Entities
{
    public class Curso
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descricao { get; set; }

        public bool Ativo { get; set; } = true;

        // Relações
        public ICollection<Aluno> Alunos { get; set; } = new List<Aluno>();
        public ICollection<Professor> Professores { get; set; } = new List<Professor>();
    }
}