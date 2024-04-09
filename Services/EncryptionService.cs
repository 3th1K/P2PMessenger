using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace P2PMessenger.Services;

public static class EncryptionService
{
    private static (byte[] Key, byte[] IV) DeriveKeyAndIV(byte[] sharedSecret, int keySize = 256)
    {
        using var hmac = new HMACSHA256(sharedSecret); // Use shared secret as the key for HMAC
        byte[] key = hmac.ComputeHash(Encoding.UTF8.GetBytes("Key Derivation"));
        byte[] iv = hmac.ComputeHash(Encoding.UTF8.GetBytes("IV Derivation"));
        return (key.Take(keySize / 8).ToArray(), iv.Take(16).ToArray()); // AES key and IV
    }

    public static byte[] EncryptStringToBytes_Aes(string plainText, byte[] sharedSecret)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentNullException(nameof(plainText));
        if (sharedSecret == null || sharedSecret.Length <= 0)
            throw new ArgumentNullException(nameof(sharedSecret));

        (byte[] key, byte[] iv) = DeriveKeyAndIV(sharedSecret);

        using Aes aesAlg = Aes.Create();
        aesAlg.Key = key;
        aesAlg.IV = iv;

        ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        using MemoryStream msEncrypt = new MemoryStream();
        using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using StreamWriter swEncrypt = new StreamWriter(csEncrypt);
        swEncrypt.Write(plainText);
        swEncrypt.Flush();
        csEncrypt.FlushFinalBlock();
        return msEncrypt.ToArray();
    }

    public static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] sharedSecret)
    {
        if (cipherText == null || cipherText.Length <= 0)
            throw new ArgumentNullException(nameof(cipherText));
        if (sharedSecret == null || sharedSecret.Length <= 0)
            throw new ArgumentNullException(nameof(sharedSecret));

        (byte[] key, byte[] iv) = DeriveKeyAndIV(sharedSecret);

        using Aes aesAlg = Aes.Create();
        aesAlg.Key = key;
        aesAlg.IV = iv;

        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
        using MemoryStream msDecrypt = new MemoryStream(cipherText);
        using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using StreamReader srDecrypt = new StreamReader(csDecrypt);
        return srDecrypt.ReadToEnd();
    }
}
