using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SGEEP.Web.Areas.Identity.Pages.Account
{
    // Public self-registration is disabled. Accounts are provisioned by an
    // administrator (or a Course Director, for students). Both GET and POST are
    // redirected to the login page so the endpoint cannot be used to create users.
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        public IActionResult OnGet() => Redirect("/Identity/Account/Login");

        public IActionResult OnPost() => Redirect("/Identity/Account/Login");
    }
}
