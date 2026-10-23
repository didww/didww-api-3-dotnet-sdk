using Didww.Api3.Resource;
using Didww.Api3.Resource.Enums;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace Didww.Api3.Tests;

public class ExportTypesTest : BaseTest
{
    [Fact]
    public async Task TestCreateSmsInExport()
    {
        StubPost("exports", "exports/create_sms_in_request.json", "exports/create_sms_in.json");

        var export = new Export
        {
            ExportType = ExportType.SmsIn,
            Filters = new Dictionary<string, object>
            {
                ["from"] = "2026-10-01 00:00:00",
                ["to"] = "2026-10-15 00:00:00",
                ["trunk_type"] = new List<string> { ExportTrunkTypes.SmppSmsc, ExportTrunkTypes.HttpIn },
                ["destination_address_eq"] = "12125550100"
            }
        };

        var created = (await Client.Exports().CreateAsync(export)).Data;

        created.Id.Should().Be("5a2d3e4f-6b7c-4d8e-9f0a-1b2c3d4e5f60");
        created.ExportType.Should().Be(ExportType.SmsIn);
        created.Status.Should().Be(ExportStatus.Pending);
        created.Filters.Should().ContainKey("trunk_type");
        ((JArray)created.Filters!["trunk_type"]).ToObject<List<string>>().Should().Equal("SMPP SMSC", "HTTP IN");
        created.Filters["destination_address_eq"].Should().Be("12125550100");
    }

    [Fact]
    public async Task TestCreateSmsOutExport()
    {
        StubPost("exports", "exports/create_sms_out_request.json", "exports/create_sms_out.json");

        var export = new Export
        {
            ExportType = ExportType.SmsOut,
            Filters = new Dictionary<string, object>
            {
                ["from"] = "2026-10-01 00:00:00",
                ["to"] = "2026-10-15 00:00:00",
                ["sms_trunk.id"] = new List<string>
                {
                    "b2c3d4e5-0000-4000-8000-000000000001",
                    "b2c3d4e5-0000-4000-8000-000000000003"
                },
                ["service_type"] = "a2p",
                ["sender_id_verification.id"] = "c3d4e5f6-0000-4000-8000-000000000002"
            }
        };

        var created = (await Client.Exports().CreateAsync(export)).Data;

        created.Id.Should().Be("4f1c2d3e-5a6b-4c7d-8e9f-0a1b2c3d4e5f");
        created.ExportType.Should().Be(ExportType.SmsOut);
        created.Filters!["service_type"].Should().Be("a2p");
        created.Filters["sender_id_verification.id"].Should().Be("c3d4e5f6-0000-4000-8000-000000000002");
    }

    [Theory]
    [InlineData("\"cdr_in\"", ExportType.CdrIn)]
    [InlineData("\"cdr_out\"", ExportType.CdrOut)]
    [InlineData("\"sms_in\"", ExportType.SmsIn)]
    [InlineData("\"sms_out\"", ExportType.SmsOut)]
    public void TestExportTypeWireValues(string json, ExportType expected)
    {
        Newtonsoft.Json.JsonConvert.DeserializeObject<ExportType>(json).Should().Be(expected);
        Newtonsoft.Json.JsonConvert.SerializeObject(expected).Should().Be(json);
    }

    [Fact]
    public void TestExportTrunkTypeValues()
    {
        ExportTrunkTypes.SmppSmsc.Should().Be("SMPP SMSC");
        ExportTrunkTypes.SmppEsme.Should().Be("SMPP ESME");
        ExportTrunkTypes.Email.Should().Be("Email");
        ExportTrunkTypes.HttpIn.Should().Be("HTTP IN");
    }
}
