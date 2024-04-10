using System.Security.Cryptography;

namespace P2PMessenger.Security;

/// <summary>
/// Class for handling secure key exchange.
/// </summary>
public static class SharedKeyUtility
{
    /// <summary>
    /// Generate ECDH key
    /// </summary>
    /// <returns>ECDiffieHellmanCng object</returns>
    public static ECDiffieHellmanCng GenerateEcdhKey()
    {
        var ecdh = new ECDiffieHellmanCng
        {
            KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
            HashAlgorithm = CngAlgorithm.Sha256
        };
        return ecdh;
    }

    /// <summary>
    /// Fetch the public key using Elliptic Curve Diffie-Hellman (ECDH) algorithm.
    /// </summary>
    /// <param name="ecdh"></param>
    /// <returns>Public key</returns>
    public static byte[] GetPublicKey(ECDiffieHellmanCng ecdh)
    {
        return ecdh.PublicKey.ToByteArray();
    }

    /// <summary>
    /// Compute and generate shared secret using our Key and other's public key
    /// </summary>
    /// <param name="ecdh"></param>
    /// <param name="otherPublicKey"></param>
    /// <returns>Generated shared secret</returns>
    public static byte[] GenerateSharedSecret(ECDiffieHellmanCng ecdh, byte[] otherPublicKey)
    {
        using var otherPubKey = CngKey.Import(otherPublicKey, CngKeyBlobFormat.EccPublicBlob);
        return ecdh.DeriveKeyMaterial(otherPubKey);
    }
}

