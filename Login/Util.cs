using System.Security.Cryptography;
using System.Text;

namespace Login
{
    public static class Util
    {
        private const int KEY_LENGHT = 32;
        public static char[] HASH = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '_' };

        public static string GenerateLoginKey()
        {
            return GenerateString(KEY_LENGHT);
        }

        /// <summary>
        /// Genera un token aleatorio con RNG criptográfico: la clave de conexión y el ticket
        /// protegen credenciales y el handoff login→game, no pueden ser predecibles.
        /// HASH.Length (64) divide a 256, por lo que el módulo no introduce sesgo.
        /// </summary>
        public static string GenerateString(int length)
        {
            var data = new byte[length];
            RandomNumberGenerator.Fill(data);

            var str = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                str.Append(HASH[data[i] % HASH.Length]);
            }
            return str.ToString();
        }

        public static string CryptPassword(string key, string password)
        {
            int hashLen = HASH.Length;
            var crypted = new StringBuilder(password.Length * 2);

            for (int i = 0; i < password.Length; i++)
            {
                int pPass = password[i];
                // La clave mide 32 caracteres; una contraseña almacenada más larga no debe
                // desbordar el índice (el cliente aplica el mismo esquema cíclico).
                int pKey = key[i % key.Length];
                crypted.Append(HASH[((pPass >> 4) + pKey) % hashLen]);
                crypted.Append(HASH[((pPass & 0xF) + pKey) % hashLen]);
            }

            return crypted.ToString();
        }

    }
}
