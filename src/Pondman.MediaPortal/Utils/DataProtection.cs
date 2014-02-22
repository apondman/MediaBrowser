namespace Pondman.MediaPortal.Utils
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    public class DataProtection
    {
        static readonly byte[] entropyBytes = { 3, 9, 7, 4, 5 };

        public static string Encrypt(string plain)
        {
            var bytes = Encoding.UTF8.GetBytes(plain);
            var encrypted = Protect(bytes);
            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(string encrypted)
        {
            var bytes = Convert.FromBase64String(encrypted);
            var decrypted = Unprotect(bytes);
            return Encoding.UTF8.GetString(decrypted, 0, decrypted.Length);
        }
        
        public static byte[] Protect(byte[] data)
        {
            return ProtectedData.Protect(data, entropyBytes, DataProtectionScope.LocalMachine);
        }

        public static byte[] Unprotect(byte[] data)
        {
           return ProtectedData.Unprotect(data, entropyBytes, DataProtectionScope.LocalMachine);
        }

    }
}
