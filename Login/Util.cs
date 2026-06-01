using System.Text;
using Protocolo.Framework.Generic;
using Protocolo.Framework.Utils;

namespace Login
{
    public static class Util
    {
        private const int KEY_LENGHT = 32;
        public static char[] HASH = new char[] {'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' , '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '_'};
        private static FastRandom random = new FastRandom();
        public static ObjectPool<string> AuthKeyPool = new ObjectPool<string>(GenerateLoginKey, AuthService.MAX_CLIENT);

        public static string GenerateLoginKey()
        {
            return GenerateString(KEY_LENGHT);
        }

        public static string GenerateString(int length)
        {
            var str = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                str.Append(HASH[random.Next(HASH.Length)]);
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
                int pKey = key[i];
                crypted.Append(HASH[((pPass >> 4) + pKey) % hashLen]);
                crypted.Append(HASH[((pPass & 0xF) + pKey) % hashLen]);
            }

            return crypted.ToString();
        }

    }
}

