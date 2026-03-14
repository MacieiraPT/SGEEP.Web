namespace SGEEP.Web.Services
{
    public static class EmailTemplates
    {
        private const string CorPrimaria = "#1e40af";
        private const string CorPrimariaClara = "#3b82f6";
        private const string CorFundo = "#f0f4f8";
        private const string CorTexto = "#1e293b";
        private const string CorTextoSecundario = "#64748b";
        private const string CorSucesso = "#15803d";
        private const string CorErro = "#dc2626";
        private const string CorAviso = "#b45309";

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
<body style=""margin:0;padding:0;background-color:{CorFundo};font-family:'Segoe UI',Roboto,Arial,sans-serif;color:{CorTexto};-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""background-color:{CorFundo};padding:40px 16px;"">
    <tr>
      <td align=""center"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""max-width:600px;width:100%;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);"">

          <!-- Cabeçalho -->
          <tr>
            <td style=""background:linear-gradient(135deg,{CorPrimaria} 0%,{CorPrimariaClara} 100%);padding:0;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"">
                <tr>
                  <td style=""padding:32px 40px 28px 40px;text-align:center;"">
                    <table cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""margin:0 auto;"">
                      <tr>
                        <td style=""background:rgba(255,255,255,0.18);border-radius:10px;padding:8px 18px;"">
                          <span style=""font-size:26px;font-weight:800;color:#ffffff;letter-spacing:2px;text-decoration:none;"">SGEEP</span>
                        </td>
                      </tr>
                    </table>
                    <p style=""margin:12px 0 0 0;font-size:13px;color:rgba(255,255,255,0.8);letter-spacing:0.3px;line-height:1.4;"">
                      Sistema de Gestão de Estágios para Escolas Profissionais
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Barra decorativa -->
          <tr>
            <td style=""background:linear-gradient(90deg,{CorPrimaria},{CorPrimariaClara},{CorPrimaria});height:3px;font-size:0;line-height:0;"">&nbsp;</td>
          </tr>

          <!-- Conteúdo -->
          <tr>
            <td style=""background-color:#ffffff;padding:40px 44px;font-size:15px;line-height:1.75;color:{CorTexto};"">
              {conteudo}
            </td>
          </tr>

          <!-- Rodapé -->
          <tr>
            <td style=""background-color:#f8fafc;padding:24px 40px;text-align:center;border-top:1px solid #e2e8f0;"">
              <p style=""margin:0 0 4px 0;font-size:12px;color:{CorTextoSecundario};line-height:1.5;"">
                Este é um email automático — por favor não responda.
              </p>
              <p style=""margin:0;font-size:11px;color:#94a3b8;"">
                &copy; {DateTime.Now.Year} SGEEP — Sistema de Gestão de Estágios
              </p>
            </td>
          </tr>

        </table>

        <!-- Texto extra abaixo do email -->
        <p style=""margin:20px 0 0 0;font-size:11px;color:#94a3b8;text-align:center;"">
          Recebeu este email porque tem uma conta no SGEEP.
        </p>
      </td>
    </tr>
  </table>
</body>
</html>";
        }

        /// <summary>
        /// Gera uma saudação personalizada.
        /// </summary>
        public static string Saudacao(string nome)
        {
            return $@"<p style=""margin:0 0 20px 0;font-size:16px;"">Caro(a) <strong>{nome}</strong>,</p>";
        }

        /// <summary>
        /// Gera a assinatura padrão.
        /// </summary>
        public static string Assinatura()
        {
            return @"<table cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""margin-top:32px;"">
  <tr>
    <td style=""border-top:1px solid #e2e8f0;padding-top:16px;"">
      <p style=""margin:0;color:#64748b;font-size:14px;"">Cumprimentos,</p>
      <p style=""margin:4px 0 0 0;font-size:15px;font-weight:700;color:#1e40af;"">SGEEP</p>
    </td>
  </tr>
</table>";
        }

        /// <summary>
        /// Gera uma tabela de credenciais estilizada (email + password).
        /// </summary>
        public static string TabelaCredenciais(string email, string password)
        {
            return $@"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""margin:20px 0;border-radius:8px;background:#f8fafc;border:1px solid #e2e8f0;overflow:hidden;"">
  <tr>
    <td style=""padding:14px 20px;border-bottom:1px solid #e2e8f0;"">
      <table cellpadding=""0"" cellspacing=""0"" role=""presentation"">
        <tr>
          <td style=""padding-right:16px;vertical-align:middle;"">
            <span style=""font-size:13px;color:#64748b;font-weight:600;text-transform:uppercase;letter-spacing:0.5px;"">Email</span>
          </td>
          <td style=""vertical-align:middle;"">
            <span style=""font-size:15px;color:#1e293b;"">{email}</span>
          </td>
        </tr>
      </table>
    </td>
  </tr>
  <tr>
    <td style=""padding:14px 20px;"">
      <table cellpadding=""0"" cellspacing=""0"" role=""presentation"">
        <tr>
          <td style=""padding-right:16px;vertical-align:middle;"">
            <span style=""font-size:13px;color:#64748b;font-weight:600;text-transform:uppercase;letter-spacing:0.5px;"">Password</span>
          </td>
          <td style=""vertical-align:middle;"">
            <code style=""background:#1e293b;color:#e2e8f0;padding:6px 14px;border-radius:6px;font-size:14px;font-family:'Courier New',monospace;letter-spacing:1px;"">{password}</code>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>";
        }

        /// <summary>
        /// Gera uma tabela de informação com uma única linha (ex: nota final).
        /// </summary>
        public static string TabelaInfo(string rotulo, string valor, string? corValor = null, string? tamanhoValor = null)
        {
            var estiloValor = $"font-weight:700;{(corValor != null ? $"color:{corValor};" : "")}{(tamanhoValor != null ? $"font-size:{tamanhoValor};" : "font-size:15px;")}";
            return $@"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""margin:20px 0;border-radius:8px;background:#f8fafc;border:1px solid #e2e8f0;overflow:hidden;"">
  <tr>
    <td style=""padding:16px 20px;"">
      <table cellpadding=""0"" cellspacing=""0"" role=""presentation"">
        <tr>
          <td style=""padding-right:16px;vertical-align:middle;"">
            <span style=""font-size:13px;color:#64748b;font-weight:600;text-transform:uppercase;letter-spacing:0.5px;"">{rotulo}</span>
          </td>
          <td style=""vertical-align:middle;"">
            <span style=""{estiloValor}"">{valor}</span>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>";
        }

        /// <summary>
        /// Gera uma caixa de aviso (warning) estilizada.
        /// </summary>
        public static string CaixaAviso(string mensagem)
        {
            return $@"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""margin:20px 0;"">
  <tr>
    <td style=""background:#fffbeb;border:1px solid #fde68a;border-radius:8px;border-left:4px solid #f59e0b;padding:14px 18px;"">
      <table cellpadding=""0"" cellspacing=""0"" role=""presentation"">
        <tr>
          <td style=""vertical-align:middle;padding-right:10px;font-size:18px;"">&#9888;&#65039;</td>
          <td style=""vertical-align:middle;color:{CorAviso};font-size:14px;font-weight:500;line-height:1.5;"">{mensagem}</td>
        </tr>
      </table>
    </td>
  </tr>
</table>";
        }

        /// <summary>
        /// Gera um badge/etiqueta de estado (ex: "ativado", "rejeitado").
        /// </summary>
        public static string BadgeEstado(string texto, string tipo)
        {
            var (cor, fundo, borda) = tipo switch
            {
                "sucesso" => (CorSucesso, "#f0fdf4", "#bbf7d0"),
                "erro" => (CorErro, "#fef2f2", "#fecaca"),
                "info" => (CorPrimaria, "#eff6ff", "#bfdbfe"),
                _ => (CorTexto, "#f8fafc", "#e2e8f0")
            };
            return $@"<span style=""display:inline-block;background:{fundo};color:{cor};border:1px solid {borda};padding:3px 12px;border-radius:20px;font-size:14px;font-weight:700;"">{texto}</span>";
        }

        /// <summary>
        /// Gera uma caixa de comentário estilizada (ex: comentário do professor).
        /// </summary>
        public static string CaixaComentario(string rotulo, string comentario)
        {
            return $@"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""margin:20px 0;"">
  <tr>
    <td style=""background:#f8fafc;border-left:4px solid {CorPrimaria};border-radius:0 8px 8px 0;padding:16px 20px;"">
      <p style=""margin:0 0 6px 0;font-size:12px;color:#64748b;font-weight:600;text-transform:uppercase;letter-spacing:0.5px;"">{rotulo}</p>
      <p style=""margin:0;font-size:14px;color:{CorTexto};line-height:1.6;"">{comentario}</p>
    </td>
  </tr>
</table>";
        }
    }
}
