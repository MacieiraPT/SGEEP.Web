namespace SGEEP.Web.Middleware
{
    public class ForcePasswordChangeMiddleware
    {
        private readonly RequestDelegate _next;

        // Apenas os caminhos estritamente necessários: o ecrã de alteração de
        // password, o de login/logout e a página de erro. Tudo o resto é
        // redirecionado para forçar a alteração antes de aceder à aplicação.
        private static readonly string[] CaminhosPermitidos =
        {
            "/account/changepassword",
            "/identity/account/logout",
            "/identity/account/login",
            "/home/error"
        };

        // Prefixos de ficheiros estáticos. UseStaticFiles está antes deste
        // middleware no pipeline (ficheiros existentes não chegam cá), mas
        // listamos explicitamente para o caso de 404s a ficheiros estáticos.
        private static readonly string[] PrefixosEstaticos =
        {
            "/css/", "/js/", "/lib/", "/images/", "/img/", "/fonts/", "/favicon"
        };

        public ForcePasswordChangeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true
                && context.User.HasClaim("MustChangePassword", "true"))
            {
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

                var permitido =
                    CaminhosPermitidos.Any(p => path.StartsWith(p, StringComparison.Ordinal))
                    || PrefixosEstaticos.Any(p => path.StartsWith(p, StringComparison.Ordinal));

                if (!permitido)
                {
                    context.Response.Redirect("/Account/ChangePassword");
                    return;
                }
            }

            await _next(context);
        }
    }
}
