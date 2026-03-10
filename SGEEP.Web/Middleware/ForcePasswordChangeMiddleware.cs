namespace SGEEP.Web.Middleware
{
    public class ForcePasswordChangeMiddleware
    {
        private readonly RequestDelegate _next;

        public ForcePasswordChangeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var path = context.Request.Path.Value?.ToLower() ?? "";

                // Don't redirect if already on ChangePassword, logging out, or accessing static files
                var allowedPaths = new[]
                {
                    "/account/changepassword",
                    "/identity/account/logout",
                    "/identity/account/login"
                };

                var isAllowed = allowedPaths.Any(p => path.StartsWith(p))
                    || path.StartsWith("/css")
                    || path.StartsWith("/js")
                    || path.StartsWith("/lib")
                    || path.Contains(".");

                if (!isAllowed && context.User.HasClaim("MustChangePassword", "true"))
                {
                    context.Response.Redirect("/Account/ChangePassword");
                    return;
                }
            }

            await _next(context);
        }
    }
}
