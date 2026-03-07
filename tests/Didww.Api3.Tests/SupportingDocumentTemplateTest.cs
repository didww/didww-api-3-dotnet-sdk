using FluentAssertions;

namespace Didww.Api3.Tests;

public class SupportingDocumentTemplateTest : BaseTest
{
    [Fact]
    public async Task TestListSupportingDocumentTemplates()
    {
        StubGet("supporting_document_templates", "supporting_document_templates/index.json");

        var response = await Client.SupportingDocumentTemplates().ListAsync();
        var templates = response.Data;

        templates.Should().NotBeEmpty();

        var first = templates[0];
        first.Id.Should().Be("206ccec2-1166-461f-9f58-3a56823db548");
        first.Name.Should().Be("Generic LOI");
        first.Permanent.Should().BeFalse();
        first.Url.Should().NotBeNullOrEmpty();
    }
}
