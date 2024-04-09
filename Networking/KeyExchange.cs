using System.Security.Cryptography;

namespace P2PMessenger.Networking;

public static class KeyExchange
{
    public static ECDiffieHellmanCng GenerateECDHKey()
    {
        var ecdh = new ECDiffieHellmanCng
        {
            KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
            HashAlgorithm = CngAlgorithm.Sha256
        };
        return ecdh;
    }

    public static byte[] GetPublicKey(ECDiffieHellmanCng ecdh)
    {
        return ecdh.PublicKey.ToByteArray();
    }

    public static byte[] ComputeSharedSecret(ECDiffieHellmanCng ecdh, byte[] otherPublicKey)
    {
        using var otherPubKey = CngKey.Import(otherPublicKey, CngKeyBlobFormat.EccPublicBlob);
        return ecdh.DeriveKeyMaterial(otherPubKey);
    }
}

