using Didww.Api3.Http;
using Didww.Api3.Resource;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class ProofTest : BaseTest
{
    [Fact]
    public async Task TestCreateProof()
    {
        StubPost("proofs", "proofs/create_request.json", "proofs/create.json");

        var proofType = ProofType.Build("19cd7b22-559b-41d4-99c9-7ad7ad63d5d1");
        var encryptedFile = EncryptedFile.Build("254b3c2d-c40c-4ff7-93b1-a677aee7fa10");

        var proof = new Proof
        {
            ProofType = proofType,
            Files = new List<EncryptedFile> { encryptedFile }
        };

        var createParams = new QueryParams().Include("proof_type");
        var response = await Client.Proofs().CreateAsync(proof, createParams);
        var created = response.Data;

        created.Id.Should().Be("ed46925b-a830-482d-917d-015858cf7ab9");
        created.ProofType.Should().NotBeNull();
        created.ProofType!.Id.Should().Be("19cd7b22-559b-41d4-99c9-7ad7ad63d5d1");
        created.CreatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TestCreateProofWithIdentityEntity()
    {
        StubPost("proofs", "proofs/create_with_identity_request.json", "proofs/create_with_identity.json");

        var proof = new Proof
        {
            ProofType = ProofType.Build("d2c1b3fb-29f7-46ca-ba82-b617f4630b78"),
            Files = new List<EncryptedFile> { EncryptedFile.Build("cc52b6b3-0627-47d3-a1c9-b54d3de42813") },
            Entity = Identity.Build("54c92d8e-f135-4b55-ac48-748d44437509")
        };

        var response = await Client.Proofs().CreateAsync(proof);
        var created = response.Data;

        created.Id.Should().Be("84155378-88d5-456e-844d-103596e3fb2c");
        created.CreatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TestCreateProofWithAddressEntity()
    {
        StubPost("proofs", "proofs/create_with_address_request.json", "proofs/create_with_address.json");

        var proof = new Proof
        {
            ProofType = ProofType.Build("d2c1b3fb-29f7-46ca-ba82-b617f4630b78"),
            Files = new List<EncryptedFile> { EncryptedFile.Build("cc52b6b3-0627-47d3-a1c9-b54d3de42813") },
            Entity = Address.Build("54c92d8e-f135-4b55-ac48-748d44437509")
        };

        var response = await Client.Proofs().CreateAsync(proof);
        var created = response.Data;

        created.Id.Should().Be("84155378-88d5-456e-844d-103596e3fb2c");
        created.CreatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TestDeleteProof()
    {
        var id = "ed46925b-a830-482d-917d-015858cf7ab9";
        StubDelete("proofs/" + id);

        await Client.Proofs().DeleteAsync(id);
    }
}
