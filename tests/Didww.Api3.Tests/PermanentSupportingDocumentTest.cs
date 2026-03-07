using Didww.Api3.Resource;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class PermanentSupportingDocumentTest : BaseTest
{
    [Fact]
    public async Task TestCreatePermanentSupportingDocument()
    {
        StubPost("permanent_supporting_documents", "permanent_supporting_documents/create.json");

        var doc = new PermanentSupportingDocument
        {
            Identity = Identity.Build("some-identity-id"),
            Template = SupportingDocumentTemplate.Build("4199435f-646e-4e9d-a143-8f3b972b10c5"),
            Files = new List<EncryptedFile> { EncryptedFile.Build("some-file-id") }
        };

        var response = await Client.PermanentSupportingDocuments().CreateAsync(doc);
        var created = response.Data;

        created.Id.Should().Be("19510da3-c07e-4fa9-a696-6b9ab89cc172");
        created.CreatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TestDeletePermanentSupportingDocument()
    {
        var id = "19510da3-c07e-4fa9-a696-6b9ab89cc172";
        StubDelete("permanent_supporting_documents/" + id);

        await Client.PermanentSupportingDocuments().DeleteAsync(id);
    }
}
