using System.Net;

namespace SGEEP.Web.Services
{
    public static class EmailTemplates
    {
        // Todos os valores fornecidos pelo chamador podem conter dados controlados
        // pelo utilizador (nomes, comentários, emails). São sempre encoded antes
        // de serem interpolados no HTML para evitar injeção/phishing via email.
        // Os controllers devem usar Texto(...) para interpolar strings dinâmicas
        // que não passam por nenhum dos helpers abaixo.
        public static string Texto(string? valor) => WebUtility.HtmlEncode(valor ?? string.Empty);
        private static string E(string? valor) => Texto(valor);

        // Paleta alinhada com /DESIGN.md (Cal.com-inspired): canvas branca,
        // primário preto, contraste subtil. Os mesmos tokens das views.
        private const string CorPrimaria = "#111111";
        private const string CorPrimariaClara = "#242424";
        private const string CorFundo = "#ffffff";
        private const string CorCanvas = "#ffffff";
        private const string CorSuperficieSoft = "#f8f9fa";
        private const string CorSuperficieCard = "#f5f5f5";
        private const string CorHairline = "#e5e7eb";
        private const string CorHairlineSoft = "#f3f4f6";
        private const string CorTexto = "#111111";
        private const string CorBody = "#374151";
        private const string CorTextoSecundario = "#6b7280";
        private const string CorTextoTerciario = "#898989";
        private const string CorSucesso = "#10b981";
        private const string CorErro = "#ef4444";
        private const string CorAviso = "#f59e0b";

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
<body style=""margin:0;padding:0;background-color:{CorSuperficieSoft};font-family:'Inter','Segoe UI',Roboto,Arial,sans-serif;color:{CorBody};-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""background-color:{CorSuperficieSoft};padding:40px 16px;"">
    <tr>
      <td align=""center"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""max-width:600px;width:100%;background-color:{CorCanvas};border:1px solid {CorHairline};border-radius:12px;overflow:hidden;"">

          <!-- Cabeçalho -->
          <tr>
            <td style=""background-color:{CorCanvas};padding:32px 40px 16px 40px;border-bottom:1px solid {CorHairlineSoft};"">
              <span style=""font-size:22px;font-weight:600;color:{CorTexto};letter-spacing:-1px;"">SGEEP</span>
              <p style=""margin:6px 0 0 0;font-size:13px;color:{CorTextoSecundario};line-height:1.5;"">
                Sistema de Gestão de Estágios para Escolas Profissionais
              </p>
            </td>
          </tr>

          <!-- Conteúdo -->
          <tr>
            <td style=""background-color:{CorCanvas};padding:36px 40px;font-size:15px;line-height:1.7;color:{CorBody};"">
              {conteudo}
            </td>
          </tr>

          <!-- Rodapé -->
          <tr>
            <td style=""background-color:{CorSuperficieSoft};padding:20px 40px;text-align:center;border-top:1px solid {CorHairlineSoft};"">
              <p style=""margin:0 0 4px 0;font-size:12px;color:{CorTextoSecundario};line-height:1.5;"">
                Este é um email automático — por favor não responda.
              </p>
              <p style=""margin:0;font-size:11px;color:{CorTextoTerciario};"">
                &copy; {DateTime.Now.Year} SGEEP — Sistema de Gestão de Estágios
              </p>
            </td>
          </tr>

        </table>

        <p style=""margin:16px 0 0 0;font-size:11px;color:{CorTextoTerciario};text-align:center;"">
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
            return $@"<p style=""margin:0 0 20px 0;font-size:16px;"">Caro(a) <strong>{E(nome)}</strong>,</p>";
        }

        /// <summary>
        /// Gera a assinatura padrão.
        /// </summary>
        public static string Assinatura()
        {
            return $@"<table cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""margin-top:32px;"">
  <tr>
    <td style=""border-top:1px solid {CorHairlineSoft};padding-top:16px;"">
      <p style=""margin:0;color:{CorTextoSecundario};font-size:14px;"">Cumprimentos,</p>
      <p style=""margin:4px 0 0 0;font-size:15px;font-weight:600;color:{CorTexto};letter-spacing:-0.3px;"">SGEEP</p>
    </td>
  </tr>
</table>";
        }

        /// <summary>
        /// Gera uma tabela de credenciais estilizada (email + password).
        /// </summary>
        public static string TabelaCredenciais(string email, string password)
        {
            return $@"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""margin:20px 0;border-radius:8px;background:{CorSuperficieCard};border:1px solid {CorHairline};overflow:hidden;"">
  <tr>
    <td style=""padding:14px 20px;border-bottom:1px solid {CorHairline};"">
      <table cellpadding=""0"" cellspacing=""0"" role=""presentation"">
        <tr>
          <td style=""padding-right:16px;vertical-align:middle;"">
            <span style=""font-size:12px;color:{CorTextoSecundario};font-weight:600;text-transform:uppercase;letter-spacing:0.6px;"">Email</span>
          </td>
          <td style=""vertical-align:middle;"">
            <span style=""font-size:15px;color:{CorTexto};"">{E(email)}</span>
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
            <span style=""font-size:12px;color:{CorTextoSecundario};font-weight:600;text-transform:uppercase;letter-spacing:0.6px;"">Password</span>
          </td>
          <td style=""vertical-align:middle;"">
            <code style=""background:{CorTexto};color:{CorCanvas};padding:6px 14px;border-radius:6px;font-size:14px;font-family:'JetBrains Mono','Courier New',monospace;letter-spacing:1px;"">{E(password)}</code>
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
            var estiloValor = $"font-weight:600;color:{(corValor ?? CorTexto)};font-size:{(tamanhoValor ?? "15px")};letter-spacing:-0.3px;";
            return $@"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""margin:20px 0;border-radius:8px;background:{CorSuperficieCard};border:1px solid {CorHairline};overflow:hidden;"">
  <tr>
    <td style=""padding:16px 20px;"">
      <table cellpadding=""0"" cellspacing=""0"" role=""presentation"">
        <tr>
          <td style=""padding-right:16px;vertical-align:middle;"">
            <span style=""font-size:12px;color:{CorTextoSecundario};font-weight:600;text-transform:uppercase;letter-spacing:0.6px;"">{E(rotulo)}</span>
          </td>
          <td style=""vertical-align:middle;"">
            <span style=""{estiloValor}"">{E(valor)}</span>
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
    <td style=""background:{CorSuperficieSoft};border:1px solid {CorHairline};border-radius:8px;border-left:3px solid {CorAviso};padding:14px 18px;"">
      <table cellpadding=""0"" cellspacing=""0"" role=""presentation"">
        <tr>
          <td style=""vertical-align:middle;padding-right:10px;font-size:16px;color:{CorAviso};"">&#9888;&#65039;</td>
          <td style=""vertical-align:middle;color:{CorBody};font-size:14px;font-weight:500;line-height:1.5;"">{E(mensagem)}</td>
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
            var (cor, fundo) = tipo switch
            {
                "sucesso" => (CorSucesso, "rgba(16, 185, 129, 0.12)"),
                "erro" => (CorErro, "rgba(239, 68, 68, 0.12)"),
                "info" => (CorTexto, CorSuperficieCard),
                _ => (CorTexto, CorSuperficieCard)
            };
            return $@"<span style=""display:inline-block;background:{fundo};color:{cor};padding:3px 12px;border-radius:9999px;font-size:13px;font-weight:500;"">{E(texto)}</span>";
        }

        /// <summary>
        /// Gera uma caixa de comentário estilizada (ex: comentário do professor).
        /// </summary>
        public static string CaixaComentario(string rotulo, string comentario)
        {
            return $@"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""margin:20px 0;"">
  <tr>
    <td style=""background:{CorSuperficieCard};border-left:3px solid {CorTexto};border-radius:0 8px 8px 0;padding:16px 20px;"">
      <p style=""margin:0 0 6px 0;font-size:12px;color:{CorTextoSecundario};font-weight:600;text-transform:uppercase;letter-spacing:0.6px;"">{E(rotulo)}</p>
      <p style=""margin:0;font-size:14px;color:{CorBody};line-height:1.6;"">{E(comentario)}</p>
    </td>
  </tr>
</table>";
        }
    }
}
