using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Options;
using SGEEP.Web.Models;

namespace SGEEP.Web.Services
{
    public class SupabaseStorageService : IFicheiroStorageService
    {
        private readonly IAmazonS3 _s3;
        private readonly string _nomeBucket;

        public SupabaseStorageService(IOptions<SupabaseSettings> opcoes)
        {
            var config = opcoes.Value;
            _nomeBucket = config.NomeBucket;

            var credenciais = new BasicAWSCredentials(config.AccessKeyId, config.SecretAccessKey);
            _s3 = new AmazonS3Client(credenciais, new AmazonS3Config
            {
                ServiceURL = config.EndpointS3,
                ForcePathStyle = true
            });
        }

        public async Task<string> EnviarAsync(Stream conteudo, string nomeUnico, string contentType)
        {
            var request = new PutObjectRequest
            {
                BucketName = _nomeBucket,
                Key = nomeUnico,
                InputStream = conteudo,
                ContentType = contentType
            };

            await _s3.PutObjectAsync(request);
            return nomeUnico;
        }

        public async Task ApagarAsync(string caminhoFicheiro)
        {
            await _s3.DeleteObjectAsync(_nomeBucket, caminhoFicheiro);
        }

        public async Task<byte[]> DescarregarAsync(string caminhoFicheiro)
        {
            var response = await _s3.GetObjectAsync(_nomeBucket, caminhoFicheiro);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);
            return ms.ToArray();
        }
    }
}
