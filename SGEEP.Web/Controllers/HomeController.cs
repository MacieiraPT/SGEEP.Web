using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SGEEP.Web.Models;
using System.Diagnostics;

namespace SGEEP.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            return role switch
            {
                "Administrador" => RedirectToAction("Index", "Dashboard"),
                "Professor" => RedirectToAction("Professor", "Dashboard"),
                "Aluno" => RedirectToAction("Aluno", "Dashboard"),
                "Empresa" => RedirectToAction("Empresa", "Dashboard"),
                _ => RedirectToPage("/Account/Login", new { area = "Identity" })
            };
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            var code = statusCode ?? HttpContext.Response.StatusCode;

            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = code
            });
        }
    }
}