namespace SGEEP.Web.Services
{
    public interface IEmailService
    {
        Task EnviarAsync(string destinatario, string assunto, string corpo);
        Task EnviarAsync(IEnumerable<string> destinatarios, string assunto, string corpo);
    }
}
