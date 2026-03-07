using Didww.Api3.Resource;
using Didww.Api3.Resource.Enums;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class ExportTest : BaseTest
{
    [Fact]
    public async Task TestListExports()
    {
        StubGet("exports", "exports/index.json");

        var response = await Client.Exports().ListAsync();
        var exports = response.Data;

        exports.Should().NotBeEmpty();

        var first = exports[0];
        first.Id.Should().Be("da15f006-5da4-45ca-b0df-735baeadf423");
        first.ExportType.Should().Be(ExportType.CdrIn);
        first.Status.Should().Be(ExportStatus.Completed);
    }

    [Fact]
    public async Task TestFindExport()
    {
        StubGet("exports/da15f006-5da4-45ca-b0df-735baeadf423", "exports/show.json");

        var response = await Client.Exports().FindAsync("da15f006-5da4-45ca-b0df-735baeadf423");
        var export = response.Data;

        export.Id.Should().Be("da15f006-5da4-45ca-b0df-735baeadf423");
        export.ExportType.Should().Be(ExportType.CdrIn);
        export.Status.Should().Be(ExportStatus.Completed);
    }

    [Fact]
    public async Task TestCreateExport()
    {
        StubPost("exports", "exports/create.json");

        var export = new Export
        {
            ExportType = ExportType.CdrIn,
            Filters = new Dictionary<string, object>
            {
                { "year", 2024 },
                { "month", 1 }
            }
        };

        var response = await Client.Exports().CreateAsync(export);
        var created = response.Data;

        created.Id.Should().Be("da15f006-5da4-45ca-b0df-735baeadf423");
        created.ExportType.Should().Be(ExportType.CdrIn);
        created.Status.Should().Be(ExportStatus.Pending);
    }

    [Fact]
    public async Task TestCreateCdrOutExport()
    {
        StubPost("exports", "exports/create_cdr_out.json");

        var export = new Export
        {
            ExportType = ExportType.CdrOut,
            Filters = new Dictionary<string, object>
            {
                { "year", 2024 },
                { "month", 1 }
            }
        };

        var response = await Client.Exports().CreateAsync(export);
        var created = response.Data;

        created.Id.Should().Be("da15f006-5da4-45ca-b0df-735baeadf423");
        created.ExportType.Should().Be(ExportType.CdrOut);
        created.Status.Should().Be(ExportStatus.Pending);
    }
}
