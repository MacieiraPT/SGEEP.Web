using SGEEP.Core.Entities;
using SGEEP.Infrastructure.Data;

namespace SGEEP.Web.Services
{
    public class AuditoriaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditoriaService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task RegistarAsync(string acao, string entidade, int? entidadeId, string detalhes = "")
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var email = httpContext?.User?.Identity?.Name ?? "sistema";
            var ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "";

            _context.RegistosAuditoria.Add(new RegistoAuditoria
            {
                Acao = acao,
                Entidade = entidade,
                EntidadeId = entidadeId,
                Detalhes = detalhes.Length > 500 ? detalhes[..500] : detalhes,
                UtilizadorEmail = email,
                ApplicationUserId = userId,
                EnderecoIP = ip
            });

            await _context.SaveChangesAsync();
        }
    }
}
