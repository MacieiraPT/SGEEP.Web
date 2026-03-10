using System.ComponentModel.DataAnnotations;

namespace SGEEP.Web.Validation
{
    public class NifValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return new ValidationResult("O NIF é obrigatório");

            var nif = value.ToString()!.Trim();

            if (!System.Text.RegularExpressions.Regex.IsMatch(nif, @"^\d{9}$"))
                return new ValidationResult("O NIF deve ter exatamente 9 dígitos numéricos");

            // Primeiro dígito válido: 1,2,3,5,6,7,8,9
            var primeiroDigito = nif[0];
            if (!"123456789".Contains(primeiroDigito))
                return new ValidationResult("NIF inválido");

            // Algoritmo de validação do NIF português
            var soma = 0;
            for (int i = 0; i < 8; i++)
                soma += (nif[i] - '0') * (9 - i);

            var resto = soma % 11;
            var digitoControlo = resto < 2 ? 0 : 11 - resto;

            if (digitoControlo != (nif[8] - '0'))
                return new ValidationResult("NIF inválido — verifique os dígitos");

            return ValidationResult.Success;
        }
    }
}