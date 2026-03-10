using System.ComponentModel.DataAnnotations;

namespace SGEEP.Core.Entities
{
    public class Notificacao
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Mensagem { get; set; } = string.Empty;

        public bool Lida { get; set; } = false;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Utilizador destinatário
        public string ApplicationUserId { get; set; } = string.Empty;
    }
}