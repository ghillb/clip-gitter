using System.Security.Cryptography;
using System;
using System.Text;

namespace ClipGitter;
public class EncryptionManager
{
    private static readonly byte[] _salt;

    static EncryptionManager()
    {
        _salt = GenerateSalt(16);
    }

    public static string Encrypt(string plainText, string password)
    {
        using (Aes aesAlg = Aes.Create())
        {
            byte[] key = DeriveKey(password, _salt, 10000);
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
                    byte[] ciphertext = msEncrypt.ToArray();
                    byte[] iv = aesAlg.IV;
                    byte[] combined = new byte[_salt.Length + ciphertext.Length + iv.Length];
                    Array.Copy(_salt, combined, _salt.Length);
                    Array.Copy(ciphertext, 0, combined, _salt.Length, ciphertext.Length);
                    Array.Copy(iv, 0, combined, _salt.Length + ciphertext.Length, iv.Length);
                    return Convert.ToBase64String(combined);
                }
            }
        }
    }

    public static string Decrypt(string cipherText, string password)
    {
        using (Aes aesAlg = Aes.Create())
        {
            byte[] combined = Convert.FromBase64String(cipherText);
            byte[] salt = new byte[16];
            Array.Copy(combined, salt, 16);
            byte[] key = DeriveKey(password, salt, 10000);
            aesAlg.Key = key;

            // Extract IV from ciphertext
            byte[] iv = new byte[16];
            Array.Copy(combined, combined.Length - 16, iv, 0, 16);
            aesAlg.IV = iv;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            try
            {
                using (MemoryStream msDecrypt = new MemoryStream(combined, 16, combined.Length - 32))
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
