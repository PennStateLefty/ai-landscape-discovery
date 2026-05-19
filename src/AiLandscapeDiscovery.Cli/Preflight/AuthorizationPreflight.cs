using System.Text.Json;
using AiLandscapeDiscovery.Cli.Azure;

namespace AiLandscapeDiscovery.Cli.Preflight;

public sealed class AuthorizationPreflight(ArmRestClient armClient)
{
    public async Task<IReadOnlyList<AuthorizationPreflightResult>> ValidateAsync(
        IReadOnlyList<SubscriptionInfo> subscriptions,
        CancellationToken cancellationToken)
    {
        var results = new List<AuthorizationPreflightResult>();
        foreach (SubscriptionInfo subscription in subscriptions)
        {
            results.Add(await ValidateSubscriptionAsync(subscription, cancellationToken));
        }

        return results;
    }

    private async Task<AuthorizationPreflightResult> ValidateSubscriptionAsync(
        SubscriptionInfo subscription,
        CancellationToken cancellationToken)
    {
        string scope = $"/subscriptions/{subscription.SubscriptionId}";
        try
        {
            using JsonDocument document = await armClient.GetJsonAsync(
                $"{scope}/providers/Microsoft.Authorization/permissions?api-version=2022-04-01",
                cancellationToken);

            IReadOnlyList<EffectivePermission> permissions = ParsePermissions(document.RootElement);
            bool canReadResources = permissions.Any(HasRequiredReadPermission);

            return canReadResources
                ? AuthorizationPreflightResult.Success(subscription, scope, "Effective read access detected.")
                : AuthorizationPreflightResult.Failure(
                    subscription,
                    scope,
                    "The signed-in identity does not appear to have Reader, Contributor, Owner, or equivalent effective read access.");
        }
        catch (Exception ex)
        {
            return AuthorizationPreflightResult.Failure(
                subscription,
                scope,
                $"Unable to validate effective access: {ex.Message}");
        }
    }

    private static IReadOnlyList<EffectivePermission> ParsePermissions(JsonElement root)
    {
        if (!root.TryGetProperty("value", out JsonElement values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values.EnumerateArray()
            .Select(permission => new EffectivePermission(
                ReadStringArray(permission, "actions"),
                ReadStringArray(permission, "notActions")))
            .ToArray();
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values.EnumerateArray()
            .Select(value => value.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }

    public static bool HasRequiredReadPermission(EffectivePermission permission)
    {
        bool allowed = permission.Actions.Any(IsReadEnoughForDiscovery);
        bool denied = permission.NotActions.Any(IsReadEnoughForDiscovery);
        return allowed && !denied;
    }

    private static bool IsReadEnoughForDiscovery(string action)
    {
        return action.Equals("*", StringComparison.OrdinalIgnoreCase)
            || action.Equals("*/read", StringComparison.OrdinalIgnoreCase)
            || action.Equals("Microsoft.Resources/*/read", StringComparison.OrdinalIgnoreCase)
            || action.Equals("Microsoft.Resources/subscriptions/resources/read", StringComparison.OrdinalIgnoreCase)
            || action.Equals("Microsoft.Resources/subscriptions/resourceGroups/read", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record EffectivePermission(IReadOnlyList<string> Actions, IReadOnlyList<string> NotActions);

public sealed record AuthorizationPreflightResult(
    string SubscriptionId,
    string SubscriptionName,
    string TenantId,
    string Scope,
    bool Succeeded,
    string Message)
{
    public static AuthorizationPreflightResult Success(SubscriptionInfo subscription, string scope, string message)
    {
        return new AuthorizationPreflightResult(
            subscription.SubscriptionId,
            subscription.DisplayName,
            subscription.TenantId,
            scope,
            true,
            message);
    }

    public static AuthorizationPreflightResult Failure(SubscriptionInfo subscription, string scope, string message)
    {
        return new AuthorizationPreflightResult(
            subscription.SubscriptionId,
            subscription.DisplayName,
            subscription.TenantId,
            scope,
            false,
            message);
    }
}
