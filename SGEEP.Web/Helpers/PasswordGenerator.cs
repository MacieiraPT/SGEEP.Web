using System.Security.Cryptography;

namespace SGEEP.Web.Helpers
{
    public static class PasswordGenerator
    {
        // Character classes exclude visually-similar glyphs (O/0, I/l/1) so passwords
        // remain readable when delivered by email.
        private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Lower = "abcdefghijkmnpqrstuvwxyz";
        private const string Digits = "23456789";
        private const string Special = "!@#$%";
        private const string All = Upper + Lower + Digits + Special;

        public static string GerarTemporaria(int comprimento = 12)
        {
            if (comprimento < 8) comprimento = 8;

            var bytes = RandomNumberGenerator.GetBytes(comprimento);
            var chars = new char[comprimento];

            // Guarantee one of each required class (upper, digit, special).
            chars[0] = Upper[bytes[0] % Upper.Length];
            chars[1] = Digits[bytes[1] % Digits.Length];
            chars[2] = Special[bytes[2] % Special.Length];
            for (int i = 3; i < comprimento; i++)
                chars[i] = All[bytes[i] % All.Length];

            // Fisher–Yates shuffle using independent random bytes so the position of
            // the required classes is not predictable.
            var shuffle = RandomNumberGenerator.GetBytes(comprimento);
            for (int i = chars.Length - 1; i > 0; i--)
            {
                var j = shuffle[i] % (i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars);
        }
    }
}
