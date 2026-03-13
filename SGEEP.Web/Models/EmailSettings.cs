namespace SGEEP.Web.Models
{
    public class EmailSettings
    {
        public string ServidorSmtp { get; set; } = "";
        public int Porta { get; set; } = 587;
        public bool UsarSsl { get; set; } = true;
        public string Utilizador { get; set; } = "";
        public string Palavra { get; set; } = "";
        public string NomeRemetente { get; set; } = "SGEEP";
        public string EmailRemetente { get; set; } = "";
    }
}
