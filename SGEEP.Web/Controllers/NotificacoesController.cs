using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SGEEP.Web.Services;

namespace SGEEP.Web.Controllers
{
    [Authorize]
    public class NotificacoesController : Controller
    {
        private readonly NotificacaoService _notificacaoService;
        private readonly UserManager<IdentityUser> _userManager;

        public NotificacoesController(
            NotificacaoService notificacaoService,
            UserManager<IdentityUser> userManager)
        {
            _notificacaoService = notificacaoService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var notificacoes = await _notificacaoService.ObterUltimasAsync(userId, 50);
            return View(notificacoes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarLida(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            await _notificacaoService.MarcarComoLidaAsync(id, userId);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarTodasLidas()
        {
            var userId = _userManager.GetUserId(User)!;
            await _notificacaoService.MarcarTodasComoLidasAsync(userId);
            return RedirectToAction("Index");
        }
    }
}
