using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FluentAssertions;

namespace Didww.Api3.Tests;

/// <summary>
/// Tests that change the process umask: they run alone, after the parallel tests.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessUmaskCollection
{
    public const string Name = "Process umask";
}

/// <summary>A fact skipped on Windows, which has no Unix file modes.</summary>
public sealed class UnixFactAttribute : FactAttribute
{
    public UnixFactAttribute()
    {
        if (OperatingSystem.IsWindows())
            Skip = "Unix file modes only";
    }
}

[Collection(ProcessUmaskCollection.Name)]
[UnsupportedOSPlatform("windows")]
public class PricesFileUmaskTest
{
    [DllImport("libc", EntryPoint = "umask")]
    private static extern uint SetUmask(uint mask);

    [UnixFact]
    public async Task TestPriceFileDownloadToFileFollowsUmask()
    {
        var fixture = PricesServer.VoiceInFixture();
        using var server = new PricesServer((_, request) => PricesServer.PriceFile(request, fixture));
        var client = server.NewClient();
        using var dir = new TempDirectory();
        var newPath = dir.File("voice_in_prices.csv.gz");
        var oldPath = dir.File("voice_out_prices.csv.gz");
        File.WriteAllText(oldPath, "old copy");
        File.SetUnixFileMode(oldPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        var previous = SetUmask(0b000_010_111); // 027
        try
        {
            await client.VoiceInPrices().DownloadToFileAsync(newPath);
            await client.VoiceInPrices().DownloadToFileAsync(oldPath);
        }
        finally
        {
            SetUmask(previous & 0b111_111_111);
        }

        const UnixFileMode mode0640 = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead;
        File.GetUnixFileMode(newPath).Should().Be(mode0640);
        File.GetUnixFileMode(oldPath).Should().Be(mode0640, "the replaced 0600 file gets the mode of a new file");
        File.ReadAllBytes(oldPath).Should().Equal(fixture);
    }
}
