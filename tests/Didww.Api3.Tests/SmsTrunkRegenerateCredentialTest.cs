using Didww.Api3.Exception;
using Didww.Api3.Resource;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class SmsTrunkRegenerateCredentialTest : BaseTest
{
    [Fact]
    public async Task TestCreateSmsTrunkRegenerateCredential()
    {
        // Request body carries only the sms_trunk relationship: no id, no attributes.
        StubPost("sms_trunk_regenerate_credentials",
            "sms_trunk_regenerate_credentials/create_request.json",
            "sms_trunk_regenerate_credentials/create.json");

        var regenerate = new SmsTrunkRegenerateCredential
        {
            SmsTrunk = SmsTrunk.Build("1a2b3c4d-0003-4a5b-8c6d-7e8f9a0b1c03")
        };

        var response = await Client.SmsTrunkRegenerateCredentials().CreateAsync(regenerate);

        response.Data.Id.Should().Be("3c4d5e6f-0001-4c7d-8e9f-0a1b2c3d4e01");
    }

    [Fact]
    public async Task TestCreateSmsTrunkRegenerateCredentialError()
    {
        StubPost("sms_trunk_regenerate_credentials",
            "sms_trunk_regenerate_credentials/create_error.json", 422);

        var regenerate = new SmsTrunkRegenerateCredential
        {
            SmsTrunk = SmsTrunk.Build("1a2b3c4d-0002-4a5b-8c6d-7e8f9a0b1c02")
        };

        var act = () => Client.SmsTrunkRegenerateCredentials().CreateAsync(regenerate);

        var ex = await act.Should().ThrowAsync<DidwwApiException>();
        ex.Which.HttpStatus.Should().Be(422);
        ex.Which.Errors.Should().HaveCount(1);
        ex.Which.Errors[0].Source!["pointer"].ToString().Should().Be("/data/relationships/sms_trunk");
    }
}
