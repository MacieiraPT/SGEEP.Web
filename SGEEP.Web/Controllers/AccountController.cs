using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGEEP.Infrastructure.Data;
using SGEEP.Web.Models.ViewModels;
using SGEEP.Web.Services;
using System.Security.Claims;
using System.Security.Cryptography;

namespace SGEEP.Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoria;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context,
            AuditoriaService auditoria)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _auditoria = auditoria;
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

        // GET: Account/ResetPassword?userId=xxx
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            ViewBag.TargetEmail = user.Email;
            ViewBag.TargetUserId = userId;
            return View();
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ResetPassword(string userId, string dummy)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Generate new random password
            var novaPassword = GerarPasswordTemporaria();

            // Remove old password and set new one
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, novaPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                ViewBag.TargetEmail = user.Email;
                ViewBag.TargetUserId = userId;
                return View();
            }

            // Force password change on next login
            var existingClaims = await _userManager.GetClaimsAsync(user);
            if (!existingClaims.Any(c => c.Type == "MustChangePassword"))
                await _userManager.AddClaimAsync(user, new Claim("MustChangePassword", "true"));

            // Unlock account if it was locked
            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            await _auditoria.RegistarAsync("ResetPassword", "Utilizador", null, $"Password do utilizador '{user.Email}' foi reposta pelo administrador");

            TempData["Sucesso"] = $"Password de {user.Email} reposta! Nova password temporária: {novaPassword}";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/GerirUtilizadores
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GerirUtilizadores(string? pesquisa)
        {
            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(pesquisa))
                users = users.Where(u => u.Email!.Contains(pesquisa) || u.UserName!.Contains(pesquisa));

            var lista = await users.OrderBy(u => u.Email).Take(50).ToListAsync();
            ViewBag.Pesquisa = pesquisa;
            return View(lista);
        }

        private static string GerarPasswordTemporaria()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%";
            const string all = upper + lower + digits + special;

            var bytes = RandomNumberGenerator.GetBytes(12);
            var chars = new char[12];
            chars[0] = upper[bytes[0] % upper.Length];
            chars[1] = digits[bytes[1] % digits.Length];
            chars[2] = special[bytes[2] % special.Length];
            for (int i = 3; i < 12; i++)
                chars[i] = all[bytes[i] % all.Length];

            for (int i = chars.Length - 1; i > 0; i--)
            {
                var j = bytes[i % bytes.Length] % (i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars);
        }
    }
}
