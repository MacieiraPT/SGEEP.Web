namespace SGEEP.Web.Helpers
{
    public static class ValidadorFicheiro
    {
        // Lê pelo menos `minimo` bytes do início do stream. Necessário porque
        // Stream.ReadAsync permite leituras parciais — a versão anterior assumia
        // que um único Read enchia sempre o buffer, o que permitia bypass do
        // magic-byte em uploads curtos ou em chunks.
        public static async Task<byte[]> LerCabecalhoAsync(Stream stream, int minimo, CancellationToken ct = default)
        {
            var buffer = new byte[minimo];
            int total = 0;
            while (total < minimo)
            {
                var lidos = await stream.ReadAsync(buffer.AsMemory(total, minimo - total), ct);
                if (lidos == 0) break;
                total += lidos;
            }
            return total < minimo ? System.Array.Empty<byte>() : buffer;
        }

        public static bool ValidarMagicBytes(byte[] headerBytes, string extensao)
        {
            if (headerBytes is null || headerBytes.Length < 4) return false;

            return extensao.ToLowerInvariant() switch
            {
                ".pdf" => headerBytes[0] == 0x25 && headerBytes[1] == 0x50 &&
                          headerBytes[2] == 0x44 && headerBytes[3] == 0x46, // %PDF
                ".docx" => headerBytes[0] == 0x50 && headerBytes[1] == 0x4B &&
                           headerBytes[2] == 0x03 && headerBytes[3] == 0x04, // PK zip
                ".doc" => headerBytes[0] == 0xD0 && headerBytes[1] == 0xCF &&
                          headerBytes[2] == 0x11 && headerBytes[3] == 0xE0, // OLE compound
                _ => false
            };
        }
    }
}
