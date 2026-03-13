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
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context,
            AuditoriaService auditoria,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _auditoria = auditoria;
            _emailService = emailService;
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

            await _auditoria.RegistarAsync("AlterarPassword", "Utilizador", null, $"Utilizador '{user.Email}' alterou a sua password");

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

            // Enviar email com nova password
            if (!string.IsNullOrEmpty(user.Email))
                await _emailService.EnviarAsync(user.Email,
                    "SGEEP — Password Reposta",
                    EmailTemplates.Envolver(
                        $"<p>A sua password no SGEEP foi reposta pelo administrador.</p>" +
                        $"<table style=\"margin:16px 0;border-radius:6px;background:#f8fafc;border:1px solid #e2e8f0;padding:16px 20px;border-collapse:collapse;\">" +
                        $"<tr><td style=\"padding:4px 12px 4px 0;\"><strong>Email:</strong></td><td>{user.Email}</td></tr>" +
                        $"<tr><td style=\"padding:4px 12px 4px 0;\"><strong>Nova password temporária:</strong></td><td><code style=\"background:#f1f5f9;padding:2px 8px;border-radius:4px;font-size:14px;\">{novaPassword}</code></td></tr>" +
                        $"</table>" +
                        $"<p style=\"color:#b45309;background:#fef3c7;border:1px solid #fde68a;border-radius:6px;padding:12px 16px;\">&#9888; Deverá alterar a password no pr&oacute;ximo acesso.</p>" +
                        $"<p style=\"margin-top:24px;\">Cumprimentos,<br/><strong>SGEEP</strong></p>"));

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
