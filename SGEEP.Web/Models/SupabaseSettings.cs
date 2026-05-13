namespace SGEEP.Web.Models
{
    public class SupabaseSettings
    {
        // URL do projeto Supabase, p.ex. "https://abcxyz.supabase.co".
        // Visível em Dashboard → Project Settings → API → Project URL.
        public string Url { get; set; } = "";

        // service_role API key. Bypassa RLS; usar APENAS no servidor, nunca enviar
        // ao cliente. Dashboard → Project Settings → API → service_role secret.
        // Preferir variável de ambiente: Supabase__ServiceKey
        public string ServiceKey { get; set; } = "";

        // Bucket onde os relatórios são guardados. Deve existir no projeto
        // Supabase (criar manualmente em Dashboard → Storage) e estar como privado.
        public string NomeBucket { get; set; } = "relatorios";

        // Tempo de vida (segundos) dos URLs assinados de download. 60s é
        // suficiente para o utilizador clicar e o browser iniciar a descarga;
        // mais que isso aumenta a janela em que o link pode ser partilhado.
        public int SignedUrlSegundos { get; set; } = 60;

        // Timeout (segundos) para operações de upload contra o Supabase. Os
        // ficheiros têm um limite de 10MB no controller, 60s é generoso.
        public int TimeoutSegundos { get; set; } = 60;
    }
}
