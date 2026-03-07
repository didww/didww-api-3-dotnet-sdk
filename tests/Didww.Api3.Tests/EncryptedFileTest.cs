using FluentAssertions;

namespace Didww.Api3.Tests;

public class EncryptedFileTest : BaseTest
{
    [Fact]
    public async Task TestListEncryptedFiles()
    {
        StubGet("encrypted_files", "encrypted_files/index.json");

        var response = await Client.EncryptedFiles().ListAsync();
        var files = response.Data;

        files.Should().NotBeEmpty();

        var first = files[0];
        first.Id.Should().Be("7f2fbdca-8008-44ce-bcb6-3537ea5efaac");
        first.Description.Should().Be("file.enc");
        first.ExpireAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TestFindEncryptedFile()
    {
        StubGet("encrypted_files/6eed102c-66a9-4a9b-a95f-4312d70ec12a", "encrypted_files/show.json");

        var response = await Client.EncryptedFiles().FindAsync("6eed102c-66a9-4a9b-a95f-4312d70ec12a");
        var file = response.Data;

        file.Id.Should().Be("6eed102c-66a9-4a9b-a95f-4312d70ec12a");
        file.Description.Should().Be("some description");
    }

    [Fact]
    public async Task TestDeleteEncryptedFile()
    {
        var id = "6eed102c-66a9-4a9b-a95f-4312d70ec12a";
        StubDelete("encrypted_files/" + id);

        await Client.EncryptedFiles().DeleteAsync(id);
    }
}
