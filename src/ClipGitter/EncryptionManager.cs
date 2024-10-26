using System.Security.Cryptography;

namespace ClipGitter
{
    public class EncryptionManager
    {
        public static string Encrypt(string plainText, string password)
        {
            using (Aes aesAlg = Aes.Create())
            {
                byte[] salt = GenerateSalt(16);
                byte[] key = DeriveKey(password, salt, 10000);
                aesAlg.Key = key;
                aesAlg.IV = GenerateSalt(16);

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
        }

        public static string Decrypt(string cipherText, string password)
        {
            using (Aes aesAlg = Aes.Create())
            {
                byte[] salt = GenerateSalt(16);
                byte[] key = DeriveKey(password, salt, 10000);
                aesAlg.Key = key;
                aesAlg.IV = GenerateSalt(16);

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                try
                {
                    using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
                catch
                {
                    return "Decryption failed. Please provide the correct password.";
                }
            }
        }

        private static byte[] GenerateSalt(int length)
        {
            byte[] salt = new byte[length];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        private static byte[] DeriveKey(string password, byte[] salt, int iterations)
        {
            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32);
            }
        }
    }
}
