namespace SGEEP.Web.Services
{
    public interface IFicheiroStorageService
    {
        /// <summary>
        /// Envia um ficheiro para o armazenamento e devolve o caminho/identificador guardado.
        /// </summary>
        Task<string> EnviarAsync(Stream conteudo, string nomeUnico, string contentType, CancellationToken ct = default);

        /// <summary>
        /// Remove um ficheiro do armazenamento. Tolera ficheiros já inexistentes.
        /// </summary>
        Task ApagarAsync(string caminhoFicheiro, CancellationToken ct = default);

        /// <summary>
        /// Abre um ficheiro do armazenamento como stream. O caller é responsável
        /// por libertar (dispose) o stream — em ASP.NET Core, devolver o stream
        /// num FileStreamResult trata disto automaticamente.
        /// </summary>
        Task<Stream> AbrirAsync(string caminhoFicheiro, CancellationToken ct = default);
    }
}
