using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SGEEP.Web.Models;

namespace SGEEP.Web.Services
{
    public class SupabaseStorageService : IFicheiroStorageService, IDisposable
    {
        private readonly IAmazonS3 _s3;
        private readonly string _nomeBucket;
        private readonly ILogger<SupabaseStorageService> _logger;

        public SupabaseStorageService(
            IOptions<SupabaseSettings> opcoes,
            ILogger<SupabaseStorageService> logger)
        {
            var config = opcoes.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(config.EndpointS3))
                throw new InvalidOperationException("Supabase:EndpointS3 não está configurado.");
            if (string.IsNullOrWhiteSpace(config.AccessKeyId) || string.IsNullOrWhiteSpace(config.SecretAccessKey))
                throw new InvalidOperationException("Supabase:AccessKeyId/SecretAccessKey não estão configurados.");
            if (string.IsNullOrWhiteSpace(config.NomeBucket))
                throw new InvalidOperationException("Supabase:NomeBucket não está configurado.");

            _nomeBucket = config.NomeBucket;

            var credenciais = new BasicAWSCredentials(config.AccessKeyId, config.SecretAccessKey);
            _s3 = new AmazonS3Client(credenciais, new AmazonS3Config
            {
                ServiceURL = config.EndpointS3,
                ForcePathStyle = true,
                AuthenticationRegion = config.Regiao,
                Timeout = TimeSpan.FromSeconds(Math.Max(1, config.TimeoutSegundos)),
                MaxErrorRetry = 2
            });
        }

        public async Task<string> EnviarAsync(Stream conteudo, string nomeUnico, string contentType, CancellationToken ct = default)
        {
            var request = new PutObjectRequest
            {
                BucketName = _nomeBucket,
                Key = nomeUnico,
                InputStream = conteudo,
                ContentType = contentType,
                // O caller é dono do stream original (IFormFile.OpenReadStream());
                // não deixar o SDK fechá-lo permite-nos lidar com `using` no controller.
                AutoCloseStream = false
            };

            try
            {
                await _s3.PutObjectAsync(request, ct);
                return nomeUnico;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar ficheiro {Key} para o bucket {Bucket} (StatusCode={Status})",
                    nomeUnico, _nomeBucket, ex.StatusCode);
                throw;
            }
        }

        public async Task ApagarAsync(string caminhoFicheiro, CancellationToken ct = default)
        {
            try
            {
                await _s3.DeleteObjectAsync(_nomeBucket, caminhoFicheiro, ct);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Idempotente: apagar algo que já não existe não é um erro a propagar
                // — o objetivo (deixar de existir) está cumprido.
                _logger.LogDebug("Ficheiro {Key} já não existia no bucket {Bucket}.", caminhoFicheiro, _nomeBucket);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Falha ao apagar ficheiro {Key} no bucket {Bucket} (StatusCode={Status})",
                    caminhoFicheiro, _nomeBucket, ex.StatusCode);
                throw;
            }
        }

        public async Task<Stream> AbrirAsync(string caminhoFicheiro, CancellationToken ct = default)
        {
            try
            {
                var response = await _s3.GetObjectAsync(_nomeBucket, caminhoFicheiro, ct);
                // Devolvemos o stream da resposta diretamente. FileStreamResult na
                // ASP.NET Core encarrega-se de o fechar depois de servir a resposta.
                // Não buffer-amos para memória — relatórios podem ter até 10MB e o
                // servidor pode ter dezenas de downloads concorrentes.
                return response.ResponseStream;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Falha ao abrir ficheiro {Key} no bucket {Bucket} (StatusCode={Status})",
                    caminhoFicheiro, _nomeBucket, ex.StatusCode);
                throw;
            }
        }

        public void Dispose() => _s3?.Dispose();
    }
}
