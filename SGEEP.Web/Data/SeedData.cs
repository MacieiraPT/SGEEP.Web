using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace SGEEP.Web.Data
{
    public static class SeedData
    {
        public static async Task InicializarAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var configuration = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

            string[] roles = { "Administrador", "Professor", "Aluno", "Empresa" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@sgeep.pt";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser != null) return;

            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            // Prefer an operator-supplied password from configuration / user secrets / env vars.
            // If none is provided, generate a strong random password and log it once so the
            // operator can capture it. Never ship a hard-coded default.
            var configuredPassword = configuration["Seed:AdminPassword"];
            var adminPassword = string.IsNullOrWhiteSpace(configuredPassword)
                ? GerarPasswordAleatoria()
                : configuredPassword;

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Falha ao criar utilizador administrador inicial: {Erros}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(adminUser, "Administrador");
            await userManager.AddClaimAsync(adminUser, new Claim("MustChangePassword", "true"));

            if (string.IsNullOrWhiteSpace(configuredPassword))
            {
                logger.LogWarning(
                    "Administrador inicial criado. Email: {Email}. Password temporária: {Password}. " +
                    "Deve ser alterada no primeiro acesso.",
                    adminEmail, adminPassword);
            }
            else
            {
                logger.LogInformation("Administrador inicial criado a partir da configuração (Seed:AdminPassword).");
            }
        }

        private static string GerarPasswordAleatoria()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%";
            const string all = upper + lower + digits + special;

            var bytes = RandomNumberGenerator.GetBytes(16);
            var chars = new char[16];
            chars[0] = upper[bytes[0] % upper.Length];
            chars[1] = digits[bytes[1] % digits.Length];
            chars[2] = special[bytes[2] % special.Length];
            for (int i = 3; i < 16; i++)
                chars[i] = all[bytes[i] % all.Length];
            return new string(chars);
        }
    }
}
