using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace JJA.Anperi.Server.Utility
{
    public static class Cryptography
    {
        public static string CreateAuthToken(int length = 128)
        {
            string token;
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] buffer = new byte[length];
                rng.GetBytes(buffer);
                token = Convert.ToBase64String(buffer);
            }
            return token;
        }
    }
}
