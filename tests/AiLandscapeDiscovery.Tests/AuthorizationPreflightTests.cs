using AiLandscapeDiscovery.Cli.Preflight;

namespace AiLandscapeDiscovery.Tests;

public sealed class AuthorizationPreflightTests
{
    [Fact]
    public void HasRequiredReadPermissionAllowsReaderStyleWildcardRead()
    {
        var permission = new EffectivePermission(["*/read"], []);

        Assert.True(AuthorizationPreflight.HasRequiredReadPermission(permission));
    }

    [Fact]
    public void HasRequiredReadPermissionAllowsContributorStyleWildcard()
    {
        var permission = new EffectivePermission(["*"], ["Microsoft.Authorization/*/Delete"]);

        Assert.True(AuthorizationPreflight.HasRequiredReadPermission(permission));
    }
}
