using Didww.Api3.Resource;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class PermanentSupportingDocumentTest : BaseTest
{
    [Fact]
    public async Task TestCreatePermanentSupportingDocument()
    {
        StubPost("permanent_supporting_documents", "permanent_supporting_documents/create_request.json", "permanent_supporting_documents/create.json");

        var doc = new PermanentSupportingDocument
        {
            Identity = Identity.Build("5e9df058-50d2-4e34-b0d4-d1746b86f41a"),
            Template = SupportingDocumentTemplate.Build("4199435f-646e-4e9d-a143-8f3b972b10c5"),
            Files = new List<EncryptedFile> { EncryptedFile.Build("254b3c2d-c40c-4ff7-93b1-a677aee7fa10") }
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
