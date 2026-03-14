namespace SGEEP.Web.Services
{
    public static class EmailTemplates
    {
        private const string CorPrimaria = "#1d4ed8";
        private const string CorFundo = "#f1f5f9";
        private const string CorTexto = "#1e293b";
        private const string CorTextoSecundario = "#64748b";

        /// <summary>
        /// Envolve o conteúdo HTML num layout de email estilizado com cabeçalho e rodapé SGEEP.
        /// </summary>
        public static string Envolver(string conteudo)
        {
            return $@"<!DOCTYPE html>
<html lang=""pt"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>SGEEP</title>
</head>
<body style=""margin:0;padding:0;background-color:{CorFundo};font-family:'Segoe UI',Arial,sans-serif;color:{CorTexto};"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:{CorFundo};padding:32px 16px;"">
    <tr>
      <td align=""center"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;width:100%;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);"">

          <!-- Cabeçalho -->
          <tr>
            <td style=""background-color:{CorPrimaria};padding:28px 40px;text-align:center;"">
              <span style=""font-size:22px;font-weight:700;color:#ffffff;letter-spacing:1px;"">SGEEP</span>
              <br/>
              <span style=""font-size:12px;color:#bfdbfe;letter-spacing:0.5px;"">Sistema de Gestão de Estágios para Escolas Profissionais</span>
            </td>
          </tr>

          <!-- Conteúdo -->
          <tr>
            <td style=""background-color:#ffffff;padding:36px 40px;font-size:15px;line-height:1.7;color:{CorTexto};"">
              {conteudo}
            </td>
          </tr>

          <!-- Rodapé -->
          <tr>
            <td style=""background-color:{CorFundo};padding:20px 40px;text-align:center;border-top:1px solid #e2e8f0;"">
              <p style=""margin:0;font-size:12px;color:{CorTextoSecundario};"">
                Este é um email automático — por favor não responda.<br/>
                &copy; {DateTime.Now.Year} SGEEP
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }
    }
}
