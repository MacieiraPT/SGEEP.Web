namespace SGEEP.Web.Services
{
    public interface IFicheiroStorageService
    {
        /// <summary>
        /// Envia um ficheiro para o armazenamento e devolve o caminho/identificador guardado.
        /// </summary>
        Task<string> EnviarAsync(Stream conteudo, string nomeUnico, string contentType);

        /// <summary>
        /// Remove um ficheiro do armazenamento.
        /// </summary>
        Task ApagarAsync(string caminhoFicheiro);

        /// <summary>
        /// Descarrega o conteúdo de um ficheiro do armazenamento.
        /// </summary>
        Task<byte[]> DescarregarAsync(string caminhoFicheiro);
    }
}
