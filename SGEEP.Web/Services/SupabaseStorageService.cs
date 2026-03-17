using Microsoft.Extensions.Options;
using Supabase;
using SGEEP.Web.Models;

namespace SGEEP.Web.Services
{
    public class SupabaseStorageService : IFicheiroStorageService
    {
        private readonly Client _supabase;
        private readonly string _nomeBucket;

        public SupabaseStorageService(IOptions<SupabaseSettings> opcoes)
        {
            var config = opcoes.Value;
            _nomeBucket = config.NomeBucket;

            _supabase = new Client(config.Url, config.ChaveServico, new SupabaseOptions
            {
                AutoRefreshToken = false,
                AutoConnectRealtime = false
            });
            _supabase.InitializeAsync().GetAwaiter().GetResult();
        }

        public async Task<string> EnviarAsync(Stream conteudo, string nomeUnico, string contentType)
        {
            using var ms = new MemoryStream();
            await conteudo.CopyToAsync(ms);
            var bytes = ms.ToArray();

            await _supabase.Storage
                .From(_nomeBucket)
                .Upload(bytes, nomeUnico, new Supabase.Storage.FileOptions
                {
                    ContentType = contentType,
                    Upsert = false
                });

            return nomeUnico;
        }

        public async Task ApagarAsync(string caminhoFicheiro)
        {
            await _supabase.Storage
                .From(_nomeBucket)
                .Remove(new List<string> { caminhoFicheiro });
        }

        public async Task<byte[]> DescarregarAsync(string caminhoFicheiro)
        {
            return await _supabase.Storage
                .From(_nomeBucket)
                .Download(caminhoFicheiro, null);
        }
    }
}
