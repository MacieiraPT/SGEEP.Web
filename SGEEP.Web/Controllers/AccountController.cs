using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SGEEP.Web.Models.ViewModels;

namespace SGEEP.Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Account/ChangePassword
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var result = await _userManager.ChangePasswordAsync(user, vm.CurrentPassword, vm.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(vm);
            }

            // Remove the MustChangePassword claim
            var claims = await _userManager.GetClaimsAsync(user);
            var mustChangeClaim = claims.FirstOrDefault(c => c.Type == "MustChangePassword");
            if (mustChangeClaim != null)
                await _userManager.RemoveClaimAsync(user, mustChangeClaim);

            // Re-sign in to refresh the cookie (without the claim)
            await _signInManager.RefreshSignInAsync(user);

            TempData["Sucesso"] = "Password alterada com sucesso!";
            return RedirectToAction("Index", "Home");
        }
    }
}
