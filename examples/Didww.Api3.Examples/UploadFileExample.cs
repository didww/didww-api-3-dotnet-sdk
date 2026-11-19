namespace Didww.Api3.Examples;

// Encrypts a file with the DIDWW public keys and uploads it to encrypted_files,
// one file per request. The returned ID is what a proof refers to (see
// IdentityAddressProofsExample); this example deletes the file again at the end.
//
// Usage: ... -- upload-file [file]
// Without a file, a generated one-page PDF is uploaded.
public static class UploadFileExample
{
    public static async Task RunAsync(DidwwClient client, string? filePath = null)
    {
        Console.WriteLine("--- Upload Encrypted File ---");
        byte[] fileData;
        string fileName;
        if (filePath != null)
        {
            fileData = await File.ReadAllBytesAsync(filePath);
            fileName = Path.GetFileName(filePath);
        }
        else
        {
            fileData = SamplePdf.Create("DIDWW upload example");
            fileName = "sample.pdf";
            Console.WriteLine("  No file given, uploading a generated sample PDF");
        }

        // Encrypt reads the two DIDWW public keys
        var encrypt = new Encrypt(client);
        Console.WriteLine($"  Fingerprint: {encrypt.Fingerprint}");

        var encryptedData = encrypt.EncryptData(fileData);
        Console.WriteLine($"  Encrypted {fileData.Length} bytes -> {encryptedData.Length} bytes");

        var fileId = await client.UploadEncryptedFileAsync(
            encryptedData, fileName + ".enc", encrypt.Fingerprint, fileName);
        Console.WriteLine($"  Encrypted file ID: {fileId}");

        await client.EncryptedFiles().DeleteAsync(fileId);
        Console.WriteLine($"  Deleted encrypted file: {fileId}");
    }
}
