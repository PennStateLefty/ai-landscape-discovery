using AiLandscapeDiscovery.Cli.Runtime;
using AiLandscapeDiscovery.Cli.Auth;

namespace AiLandscapeDiscovery.Tests;

public sealed class CliOptionsTests
{
    [Fact]
    public void ParseAcceptsRepeatedAndCommaSeparatedSubscriptions()
    {
        ParseResult result = CliOptions.Parse(["scan", "--subscription", "sub-a,sub-b", "-s", "sub-c", "--auth-mode", "device-code", "--output", "out"]);

        Assert.False(result.ShouldExit);
        Assert.Equal(["sub-a", "sub-b", "sub-c"], result.Options!.SubscriptionIds);
        Assert.Equal(AuthMode.DeviceCode, result.Options.AuthMode);
        Assert.EndsWith("out", result.Options.OutputDirectory);
    }
}
