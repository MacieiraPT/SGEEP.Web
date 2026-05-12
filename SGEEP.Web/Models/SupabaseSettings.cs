namespace SGEEP.Web.Models
{
    public class SupabaseSettings
    {
        public string EndpointS3 { get; set; } = "";
        public string AccessKeyId { get; set; } = "";
        public string SecretAccessKey { get; set; } = "";
        public string NomeBucket { get; set; } = "relatorios";

        // Região S3 — Supabase exige uma região válida no header de autenticação,
        // mas o endpoint é o do projeto, não o da AWS. O default ("eu-west-1")
        // serve para projetos europeus; ajuste em appsettings se necessário.
        public string Regiao { get; set; } = "eu-west-1";

        // Timeout para operações de upload/download. Os ficheiros têm um limite
        // duro de 10MB por relatório, portanto 60s é generoso e protege contra
        // requests pendurados.
        public int TimeoutSegundos { get; set; } = 60;
    }
}
