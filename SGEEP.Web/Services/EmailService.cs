using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using SGEEP.Web.Models;

namespace SGEEP.Web.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task EnviarAsync(string destinatario, string assunto, string corpo)
        {
            await EnviarAsync(new[] { destinatario }, assunto, corpo);
        }

        public async Task EnviarAsync(IEnumerable<string> destinatarios, string assunto, string corpo)
        {
            if (string.IsNullOrEmpty(_settings.ServidorSmtp))
            {
                _logger.LogWarning("Email não enviado: servidor SMTP não configurado.");
                return;
            }

            var mensagem = new MimeMessage();
            mensagem.From.Add(new MailboxAddress(_settings.NomeRemetente, _settings.EmailRemetente));

            foreach (var dest in destinatarios)
            {
                if (!string.IsNullOrWhiteSpace(dest))
                    mensagem.To.Add(MailboxAddress.Parse(dest));
            }

            if (mensagem.To.Count == 0)
            {
                _logger.LogWarning("Email não enviado: nenhum destinatário válido.");
                return;
            }

            mensagem.Subject = assunto;
            mensagem.Body = new TextPart("html") { Text = corpo };

            try
            {
                using var cliente = new SmtpClient();
                await cliente.ConnectAsync(_settings.ServidorSmtp, _settings.Porta, _settings.UsarSsl);

                if (!string.IsNullOrEmpty(_settings.Utilizador))
                    await cliente.AuthenticateAsync(_settings.Utilizador, _settings.Palavra);

                await cliente.SendAsync(mensagem);
                await cliente.DisconnectAsync(true);

                _logger.LogInformation("Email enviado para {Destinatarios}: {Assunto}",
                    string.Join(", ", mensagem.To), assunto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar email para {Destinatarios}: {Assunto}",
                    string.Join(", ", mensagem.To), assunto);
            }
        }
    }
}
