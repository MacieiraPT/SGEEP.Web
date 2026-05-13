using Microsoft.Extensions.Options;
using SGEEP.Web.Models;
using Supabase.Storage;
using SupabaseClient = Supabase.Storage.Client;
using SupabaseFileOptions = Supabase.Storage.FileOptions;

namespace SGEEP.Web.Services
{
    /// <summary>
    /// Cliente de Storage do Supabase, usado para guardar e disponibilizar os
    /// relatórios dos estágios. Usa o package oficial <c>Supabase.Storage</c>
    /// — ver https://github.com/supabase-community/storage-csharp.
    ///
    /// Decisões:
    /// • Autentica como service_role (bypassa RLS). O segredo nunca é exposto
    ///   ao cliente — só é usado neste serviço, registado como Singleton.
    /// • Downloads são feitos via signed URL (TTL curto) e redirect HTTP, não
    ///   via streaming proxy. Poupa memória + bandwidth e mantém a autorização
    ///   no servidor (o link só é gerado depois de validar permissões).
    /// • Uploads continuam server-mediados porque queremos correr validações
    ///   (magic bytes, tamanho) antes de aceitar o ficheiro.
    /// </summary>
    public class SupabaseStorageService : IFicheiroStorageService
    {
        private readonly SupabaseClient _storage;
        private readonly SupabaseSettings _settings;
        private readonly ILogger<SupabaseStorageService> _logger;

        public SupabaseStorageService(
            IOptions<SupabaseSettings> opcoes,
            ILogger<SupabaseStorageService> logger)
        {
            _settings = opcoes.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_settings.Url))
                throw new InvalidOperationException("Supabase:Url não está configurado.");
            if (string.IsNullOrWhiteSpace(_settings.ServiceKey))
                throw new InvalidOperationException("Supabase:ServiceKey não está configurado.");
            if (string.IsNullOrWhiteSpace(_settings.NomeBucket))
                throw new InvalidOperationException("Supabase:NomeBucket não está configurado.");

            // Aceita várias formas que os utilizadores podem copiar do dashboard:
            //   https://<ref>.supabase.co
            //   https://<ref>.supabase.co/
            //   https://<ref>.supabase.co/storage/v1
            //   https://<ref>.supabase.co/storage/v1/s3      (endpoint S3)
            // Normalizamos para a base de storage REST (https://<ref>/storage/v1)
            // que é o que o cliente Supabase.Storage espera.
            var url = _settings.Url.Trim().TrimEnd('/');
            var idx = url.IndexOf("/storage/v1", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0) url = url.Substring(0, idx);
            var baseUrl = url + "/storage/v1";

            _logger.LogInformation("Supabase Storage base URL: {BaseUrl} (bucket: {Bucket})",
                baseUrl, _settings.NomeBucket);

            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {_settings.ServiceKey}",
                ["apikey"] = _settings.ServiceKey
            };

            _storage = new SupabaseClient(baseUrl, headers);
        }

        public async Task<string> EnviarAsync(Stream conteudo, string nomeUnico, string contentType, CancellationToken ct = default)
        {
            // O cliente Supabase.Storage (v2) aceita byte[] mas não Stream para
            // upload. Os relatórios estão limitados a 10MB pelo controller, por
            // isso o buffer temporário cabe em memória sem problema. Se um dia
            // o limite subir, substituir por upload em chunks via HTTP direto.
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await conteudo.CopyToAsync(ms, ct);
                bytes = ms.ToArray();
            }

            try
            {
                await _storage.From(_settings.NomeBucket).Upload(
                    bytes,
                    nomeUnico,
                    new SupabaseFileOptions
                    {
                        ContentType = contentType,
                        // CacheControl em segundos. Os relatórios não mudam após
                        // upload (path é Guid+extensão), por isso podem ser cached.
                        CacheControl = "3600",
                        Upsert = false
                    });

                return nomeUnico;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar ficheiro {Key} para o bucket {Bucket}",
                    nomeUnico, _settings.NomeBucket);
                throw;
            }
        }

        public async Task ApagarAsync(string caminhoFicheiro, CancellationToken ct = default)
        {
            try
            {
                await _storage.From(_settings.NomeBucket).Remove(new List<string> { caminhoFicheiro });
            }
            catch (Exception ex)
            {
                // Operação idempotente do ponto de vista do caller: log + propaga
                // para que o caller decida (no Create, apagamos para fazer rollback
                // de ficheiro órfão e não queremos esconder falhas).
                _logger.LogError(ex, "Falha ao apagar ficheiro {Key} no bucket {Bucket}",
                    caminhoFicheiro, _settings.NomeBucket);
                throw;
            }
        }

        public async Task<string> GerarUrlDownloadAsync(string caminhoFicheiro, string? nomeDownload = null, CancellationToken ct = default)
        {
            try
            {
                // Não passamos DownloadOptions ao cliente: a versão 2.4.1 do
                // Supabase.Storage anexa `?download=...` mesmo que o URL já tenha
                // `?token=...`, produzindo um URL inválido com dois `?` e o token
                // a falhar como "Invalid Compact JWS". Geramos o URL assinado
                // limpo e anexamos `&download=<nome>` à mão.
                var url = await _storage.From(_settings.NomeBucket).CreateSignedUrl(
                    caminhoFicheiro,
                    _settings.SignedUrlSegundos);

                if (string.IsNullOrWhiteSpace(url))
                    throw new InvalidOperationException(
                        $"Supabase não devolveu signed URL para '{caminhoFicheiro}'.");

                // Supabase.Storage 2.4.1 anexa SEMPRE um `?` literal ao URL
                // assinado, mesmo quando não há query a acrescentar. Esse `?`
                // fica colado ao fim da assinatura JWT (`...psiU?`) e o servidor
                // recusa o token com "Failed to base64url decode the signature".
                // Removemos qualquer `?`/`&` à direita antes de seguir.
                url = url.TrimEnd('?', '&');

                if (!string.IsNullOrWhiteSpace(nomeDownload))
                {
                    var separator = url.Contains('?') ? '&' : '?';
                    url = $"{url}{separator}download={Uri.EscapeDataString(nomeDownload)}";
                }

                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao gerar signed URL para {Key} no bucket {Bucket}",
                    caminhoFicheiro, _settings.NomeBucket);
                throw;
            }
        }
    }
}
