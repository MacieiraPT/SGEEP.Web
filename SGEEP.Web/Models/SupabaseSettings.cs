namespace SGEEP.Web.Models
{
    public class SupabaseSettings
    {
        public string EndpointS3 { get; set; } = "";
        public string AccessKeyId { get; set; } = "";
        public string SecretAccessKey { get; set; } = "";
        public string NomeBucket { get; set; } = "relatorios";
    }
}
