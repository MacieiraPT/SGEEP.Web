namespace SGEEP.Web.Helpers
{
    public static class ValidadorFicheiro
    {
        public static bool ValidarMagicBytes(byte[] headerBytes, string extensao)
        {
            if (headerBytes.Length < 4) return false;

            return extensao.ToLower() switch
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
