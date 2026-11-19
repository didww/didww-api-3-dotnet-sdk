namespace Didww.Api3.Examples;

public static class UploadFileExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- File Encryption ---");
        var encrypt = new Encrypt(client);
        Console.WriteLine($"  Fingerprint: {encrypt.Fingerprint}");

        var fileData = System.Text.Encoding.UTF8.GetBytes("sensitive document content");
        var encryptedData = encrypt.EncryptData(fileData);
        Console.WriteLine($"  Encrypted {fileData.Length} bytes -> {encryptedData.Length} bytes");

        var fileIds = await client.UploadEncryptedFileAsync(
            encryptedData, "document.pdf", encrypt.Fingerprint, "Test document");
        Console.WriteLine($"  Uploaded encrypted file IDs: {string.Join(", ", fileIds)}");
    }
}
