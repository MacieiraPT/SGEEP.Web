using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SGEEP.Web.Services;

namespace SGEEP.Web.ViewComponents
{
    public class NotificacaoBadgeViewComponent : ViewComponent
    {
        private readonly NotificacaoService _notificacaoService;
        private readonly UserManager<IdentityUser> _userManager;

        public NotificacaoBadgeViewComponent(
            NotificacaoService notificacaoService,
            UserManager<IdentityUser> userManager)
        {
            _notificacaoService = notificacaoService;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = HttpContext.User;
            if (user.Identity?.IsAuthenticated != true) return Content("");

            var userId = _userManager.GetUserId(user)!;
            var count = await _notificacaoService.ContarNaoLidasAsync(userId);
            return View(count);
        }
    }
}
