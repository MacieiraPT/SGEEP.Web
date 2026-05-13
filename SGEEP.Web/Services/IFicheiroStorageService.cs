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
        /// Gera um URL assinado e de vida curta que permite descarregar o ficheiro
        /// diretamente do storage do Supabase, sem passar pelo servidor.
        /// O caller é responsável por validar a autorização ANTES de chamar este método.
        /// </summary>
        /// <param name="caminhoFicheiro">Path do objeto dentro do bucket.</param>
        /// <param name="nomeDownload">Nome a aparecer na caixa de "guardar como" do browser
        /// (passado no parâmetro <c>?download=</c> do URL).</param>
        Task<string> GerarUrlDownloadAsync(string caminhoFicheiro, string? nomeDownload = null, CancellationToken ct = default);
    }
}
