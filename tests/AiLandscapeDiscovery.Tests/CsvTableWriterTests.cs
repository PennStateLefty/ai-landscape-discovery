using AiLandscapeDiscovery.Cli.Output;

namespace AiLandscapeDiscovery.Tests;

public sealed class CsvTableWriterTests
{
    [Fact]
    public void EscapeQuotesValuesWithCommasQuotesAndNewlines()
    {
        string escaped = CsvTableWriter.Escape("one,\"two\"\nthree");

        Assert.Equal("\"one,\"\"two\"\"\nthree\"", escaped);
    }
}
