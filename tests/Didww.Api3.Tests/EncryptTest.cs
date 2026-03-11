using System.Security.Cryptography;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class EncryptTest
{
    private static readonly RSA KeyPairA = RSA.Create(2048);
    private static readonly RSA KeyPairB = RSA.Create(2048);

    private static string ToPem(RSA rsa)
    {
        var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        var base64 = Convert.ToBase64String(publicKeyBytes, Base64FormattingOptions.InsertLineBreaks);
        return "-----BEGIN PUBLIC KEY-----\n" + base64 + "\n-----END PUBLIC KEY-----\n";
    }

    [Fact]
    public void TestEncryptWithKeysRoundTrip()
    {
        var pemA = ToPem(KeyPairA);
        var pemB = ToPem(KeyPairB);
        var publicKeys = new[] { pemA, pemB };

        var plaintext = "Hello, DIDWW encryption!"u8.ToArray();
        var encrypted = Encrypt.EncryptWithKeys(plaintext, publicKeys);

        // RSA-OAEP with 2048-bit key produces 256-byte output per key
        const int rsaBlockSize = 256;
        encrypted.Length.Should().BeGreaterThan(rsaBlockSize * 2);

        // Extract the three parts
        var encryptedRsaA = encrypted[..rsaBlockSize];
        var encryptedRsaB = encrypted[rsaBlockSize..(rsaBlockSize * 2)];
        var encryptedAes = encrypted[(rsaBlockSize * 2)..];

        // Decrypt AES credentials with private key A
        var aesCredentials = KeyPairA.Decrypt(encryptedRsaA, RSAEncryptionPadding.OaepSHA256);

        // AES credentials = 32-byte key + 16-byte IV = 48 bytes
        aesCredentials.Should().HaveCount(48);

        var aesKey = aesCredentials[..32];
        var aesIv = aesCredentials[32..];

        // Decrypt AES data
        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = aesIv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(encryptedAes, 0, encryptedAes.Length);

        decrypted.Should().BeEquivalentTo(plaintext);

        // Also verify key B has the same AES credentials
        var aesCredentialsB = KeyPairB.Decrypt(encryptedRsaB, RSAEncryptionPadding.OaepSHA256);
        aesCredentialsB.Should().BeEquivalentTo(aesCredentials);
    }

    [Fact]
    public void TestCalculateFingerprint()
    {
        var pemA = ToPem(KeyPairA);
        var pemB = ToPem(KeyPairB);
        var publicKeys = new[] { pemA, pemB };

        var fingerprint = Encrypt.CalculateFingerprint(publicKeys);

        // Fingerprint format: hex_sha1_a:::hex_sha1_b
        fingerprint.Should().Contain(":::");
        var parts = fingerprint.Split(":::");
        parts.Should().HaveCount(2);
        // Each SHA-1 hex digest is 40 characters
        parts[0].Should().HaveLength(40).And.MatchRegex("[0-9a-f]+");
        parts[1].Should().HaveLength(40).And.MatchRegex("[0-9a-f]+");
        // Two different keys should have different fingerprints
        parts[0].Should().NotBe(parts[1]);
    }

    [Fact]
    public void TestFingerprintIsConsistent()
    {
        var pemA = ToPem(KeyPairA);
        var pemB = ToPem(KeyPairB);
        var publicKeys = new[] { pemA, pemB };

        var fp1 = Encrypt.CalculateFingerprint(publicKeys);
        var fp2 = Encrypt.CalculateFingerprint(publicKeys);

        fp1.Should().Be(fp2);
    }

    [Fact]
    public void TestEncryptWithKeysProducesUniqueOutput()
    {
        var pemA = ToPem(KeyPairA);
        var pemB = ToPem(KeyPairB);
        var publicKeys = new[] { pemA, pemB };

        var plaintext = "Same input"u8.ToArray();
        var enc1 = Encrypt.EncryptWithKeys(plaintext, publicKeys);
        var enc2 = Encrypt.EncryptWithKeys(plaintext, publicKeys);

        // Each encryption uses random AES key + IV, so outputs differ
        enc1.Should().NotBeEquivalentTo(enc2);
    }
}

public class EncryptWithClientTest : BaseTest
{
    [Fact]
    public async Task TestEncryptViaClient()
    {
        StubGet("public_keys", "public_keys/index.json");

        var encrypt = new Encrypt(Client);
        encrypt.Fingerprint.Should().Contain(":::");
        encrypt.PublicKeys.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestEncryptResetAsync()
    {
        StubGet("public_keys", "public_keys/index.json");

        var encrypt = new Encrypt(Client);
        var fp1 = encrypt.Fingerprint;
        await encrypt.ResetAsync();
        encrypt.Fingerprint.Should().Be(fp1);
    }

    [Fact]
    public async Task TestEncryptDataViaClient()
    {
        StubGet("public_keys", "public_keys/index.json");

        var encrypt = new Encrypt(Client);
        var data = "test data"u8.ToArray();
        var encrypted = encrypt.EncryptData(data);
        encrypted.Should().NotBeEmpty();
        encrypted.Length.Should().BeGreaterThan(data.Length);
    }
}
