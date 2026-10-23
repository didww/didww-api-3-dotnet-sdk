using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Didww.Api3.Exception;
using Didww.Api3.Http;
using Didww.Api3.Resource;

namespace Didww.Api3;

public class Encrypt
{
    private readonly DidwwClient _client;
    private string[]? _publicKeys;
    private string? _fingerprint;

    public Encrypt(DidwwClient client)
    {
        _client = client;
        Reset();
    }

    public static byte[] EncryptWithKeys(byte[] data, string[] publicKeys)
    {
        try
        {
            // Generate AES-256-CBC key (32 bytes) and IV (16 bytes)
            var aesKey = RandomNumberGenerator.GetBytes(32);
            var aesIv = RandomNumberGenerator.GetBytes(16);

            // Encrypt data with AES-256-CBC
            byte[] encryptedAes;
            using (var aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.IV = aesIv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                encryptedAes = encryptor.TransformFinalBlock(data, 0, data.Length);
            }

            // Concatenate AES key + IV
            var aesCredentials = new byte[aesKey.Length + aesIv.Length];
            Buffer.BlockCopy(aesKey, 0, aesCredentials, 0, aesKey.Length);
            Buffer.BlockCopy(aesIv, 0, aesCredentials, aesKey.Length, aesIv.Length);

            // RSA-OAEP encrypt aesCredentials with each public key
            var encryptedRsaA = EncryptRsaOaep(publicKeys[0], aesCredentials);
            var encryptedRsaB = EncryptRsaOaep(publicKeys[1], aesCredentials);

            // Concatenate: rsa_a + rsa_b + aes_encrypted
            var result = new byte[encryptedRsaA.Length + encryptedRsaB.Length + encryptedAes.Length];
            Buffer.BlockCopy(encryptedRsaA, 0, result, 0, encryptedRsaA.Length);
            Buffer.BlockCopy(encryptedRsaB, 0, result, encryptedRsaA.Length, encryptedRsaB.Length);
            Buffer.BlockCopy(encryptedAes, 0, result, encryptedRsaA.Length + encryptedRsaB.Length, encryptedAes.Length);

            return result;
        }
        catch (System.Exception e) when (e is not DidwwClientException)
        {
            throw new DidwwClientException("Encryption failed", e);
        }
    }

    public static string CalculateFingerprint(string[] publicKeys)
    {
        return FingerprintFor(publicKeys[0]) + ":::" + FingerprintFor(publicKeys[1]);
    }

    public byte[] EncryptData(byte[] data)
    {
        return EncryptWithKeys(data, _publicKeys!);
    }

    public string[] PublicKeys => _publicKeys!;
    public string Fingerprint => _fingerprint!;

    public void Reset()
    {
        var response = _client.PublicKeys().ListAsync().GetAwaiter().GetResult();
        var keys = response.Data;
        _publicKeys = new[] { keys[0].Key!, keys[1].Key! };
        _fingerprint = CalculateFingerprint(_publicKeys);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client.PublicKeys().ListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var keys = response.Data;
        _publicKeys = new[] { keys[0].Key!, keys[1].Key! };
        _fingerprint = CalculateFingerprint(_publicKeys);
    }

    private static string FingerprintFor(string publicKeyPem)
    {
        try
        {
            var base64 = NormalizePublicKey(publicKeyPem);
            var publicKeyBin = Convert.FromBase64String(base64);
            var digest = SHA1.HashData(publicKeyBin);
            return Convert.ToHexString(digest).ToLowerInvariant();
        }
        catch (System.Exception e)
        {
            throw new DidwwClientException("Failed to calculate fingerprint", e);
        }
    }

    private static byte[] EncryptRsaOaep(string publicKeyPem, byte[] data)
    {
        var base64 = NormalizePublicKey(publicKeyPem);
        var keyBytes = Convert.FromBase64String(base64);

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);

        return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
    }

    private static string NormalizePublicKey(string publicKeyPem)
    {
        return Regex.Replace(
            publicKeyPem
                .Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", ""),
            @"\s", "");
    }
}
