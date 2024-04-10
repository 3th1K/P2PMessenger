using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace P2PMessenger.Security;


/// <summary>
/// Utility class to help with encryption/decryption.
/// </summary>
public static class EncryptionUtility
{
    #region Main Encryption/Decryption

    /// <summary>
    /// Main encryption method
    /// </summary>
    /// <param name="plainText">Plain text that will be encrypted</param>
    /// <param name="sharedSecret">Shared secret that will be used in encryption</param>
    /// <returns>Encrypted byte array</returns>
    public static byte[] Encrypt(string plainText, byte[] sharedSecret)
    {
        // Incorporate a nonce for uniqueness
        byte[] nonce = BitConverter.GetBytes(DateTime.UtcNow.Ticks);

        // Derive keys & iv with nonce
        var (aesKey, aesIv) = DeriveKeyAndIV(sharedSecret.Concat(nonce).ToArray());
        var (desKey, desIv) = DeriveKeyAndIV(sharedSecret.Concat(nonce).ToArray(), 192, 8);

        // Encrypt with AES & XOR - layer 1
        byte[] aesEncryption = EncryptWithAes(plainText, aesKey, aesIv);
        aesEncryption = XorWithPseudoRandomSequence(aesEncryption, aesKey);

        // Encrypt with Triple DES & XOR - layer 2
        byte[] desEncryption = EncryptWithTripleDes(aesEncryption, desKey, desIv);
        desEncryption = XorWithPseudoRandomSequence(desEncryption, desKey);

        // Include the nonce in the output to ensure uniqueness and for use in decryption
        return nonce.Concat(desEncryption).ToArray();
    }


    /// <summary>
    /// Main decryption method
    /// </summary>
    /// <param name="encryptedData">Encrypted byte array that will be decrypted</param>
    /// <param name="sharedSecret">Shared secret to be used in decryption</param>
    /// <returns>Decrypted plain text</returns>
    public static string Decrypt(byte[] encryptedDataWithNonce, byte[] sharedSecret)
    {
        // Extract the nonce from the beginning of the encrypted data
        byte[] nonce = encryptedDataWithNonce.Take(8).ToArray();  // Assuming 8 bytes for the DateTime.Ticks nonce
        byte[] encryptedData = encryptedDataWithNonce.Skip(8).ToArray();

        // Derive keys & iv with nonce
        var (aesKey, aesIv) = DeriveKeyAndIV(sharedSecret.Concat(nonce).ToArray());
        var (desKey, desIv) = DeriveKeyAndIV(sharedSecret.Concat(nonce).ToArray(), 192, 8);

        // Reverse the process: DES decryption and XOR
        encryptedData = XorWithPseudoRandomSequence(encryptedData, desKey);
        byte[] decryptedBytes = DecryptWithTripleDes(encryptedData, desKey, desIv);

        // Reverse the process: AES decryption and XOR
        decryptedBytes = XorWithPseudoRandomSequence(decryptedBytes, aesKey);
        string decrypted = DecryptWithAes(decryptedBytes, aesKey, aesIv);

        return decrypted;
    }


    #endregion

    #region AES Encryption/Decription
    /// <summary>
    /// Encrypts plain text using AES key and iv.
    /// </summary>
    /// <param name="plainText"> Plain text to be encrypted</param>
    /// <param name="aesKey">Aes Key</param>
    /// <param name="aesIV">Aes Iv</param>
    /// <returns>Encrypted byte array</returns>
    private static byte[] EncryptWithAes(string plainText, byte[] aesKey, byte[] aesIV)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = aesKey;
        aesAlg.IV = aesIV;
        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }
        return msEncrypt.ToArray();
    }

    /// <summary>
    /// Decrypts the encrypted data using Aes Key and Aes Iv
    /// </summary>
    /// <param name="cipherText">Encrypted data to be decrypted</param>
    /// <param name="key">Aes Key</param>
    /// <param name="iv">Aes Iv</param>
    /// <returns>Decrypted plain text</returns>
    private static string DecryptWithAes(byte[] cipherText, byte[] key, byte[] iv)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = key;
        aesAlg.IV = iv;
        var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        using var msDecrypt = new MemoryStream(cipherText);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        return srDecrypt.ReadToEnd();
    }

    #endregion

    #region TripleDES Encryption/Decryption
    /// <summary>
    /// Encrypts data using triple des key and iv.
    /// </summary>
    /// <param name="data">Data to be encrypted</param>
    /// <param name="tripleDesKey">triple des key</param>
    /// <param name="tripleDesIV">triple des iv</param>
    /// <returns>Encrypted data</returns>
    private static byte[] EncryptWithTripleDes(byte[] data, byte[] tripleDesKey, byte[] tripleDesIV)
    {
        using var tripleDes = TripleDES.Create();
        tripleDes.Key = tripleDesKey;
        tripleDes.IV = tripleDesIV;
        var encryptor = tripleDes.CreateEncryptor(tripleDes.Key, tripleDes.IV);

        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        {
            csEncrypt.Write(data, 0, data.Length);
        }
        return msEncrypt.ToArray();
    }
    /// <summary>
    /// Decrypts the data using triple des key and iv.
    /// </summary>
    /// <param name="cipherText">Data to be encrypted</param>
    /// <param name="key">Triple des key</param>
    /// <param name="iv">Triple des iv</param>
    /// <returns>Decrypted data</returns>
    private static byte[] DecryptWithTripleDes(byte[] cipherText, byte[] key, byte[] iv)
    {
        using var tripleDes = TripleDES.Create();
        tripleDes.Key = key;
        tripleDes.IV = iv;
        var decryptor = tripleDes.CreateDecryptor(tripleDes.Key, tripleDes.IV);

        using var msDecrypt = new MemoryStream();
        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
        {
            csDecrypt.Write(cipherText, 0, cipherText.Length);
            csDecrypt.FlushFinalBlock(); // Ensure all data is written to the MemoryStream
        }

        return msDecrypt.ToArray();
    }

    #endregion

    #region Helpers Encryption/Decryption
    /// <summary>
    /// Derive Key and Iv using shared secret
    /// </summary>
    /// <param name="sharedSecret">Shared secret to be used for derivation</param>
    /// <param name="keySize">Key size</param>
    /// <param name="ivSize">Iv size</param>
    /// <returns>Derived Key and Iv</returns>
    private static (byte[] Key, byte[] IV) DeriveKeyAndIV(byte[] sharedSecret, int keySize = 256, int ivSize = 16)
    {
        using var hmac = new HMACSHA256(sharedSecret); // Use shared secret as the key for HMAC
        byte[] key = hmac.ComputeHash(Encoding.UTF8.GetBytes("Key Derivation"));
        byte[] iv = hmac.ComputeHash(Encoding.UTF8.GetBytes("IV Derivation"));
        return (key.Take(keySize / 8).ToArray(), iv.Take(ivSize).ToArray()); // AES key and IV
    }

    /// <summary>
    /// Xor the data with a pseudo random sequence of the key provided.
    /// </summary>
    /// <param name="data">Data to be XORed</param>
    /// <param name="key">Key to be XORed with</param>
    /// <returns>XORed data</returns>
    private static byte[] XorWithPseudoRandomSequence(byte[] data, byte[] key)
    {
        using (var hmac = new HMACSHA256(key)) // Using HMAC to generate a deterministic pseudo-random sequence based on the key
        {
            var pseudoRandomSequence = hmac.ComputeHash(Encoding.UTF8.GetBytes("XOR sequence")); // Deterministic sequence
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= pseudoRandomSequence[i % pseudoRandomSequence.Length];
            }
        }
        return data;
    }

    #endregion

}
