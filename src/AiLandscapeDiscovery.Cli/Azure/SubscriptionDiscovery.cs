using System.Text.Json;

namespace AiLandscapeDiscovery.Cli.Azure;

public sealed class SubscriptionDiscovery(ArmRestClient armClient)
{
    public async Task<IReadOnlyList<SubscriptionInfo>> ResolveAsync(
        IReadOnlyList<string> subscriptionIds,
        CancellationToken cancellationToken)
    {
        if (subscriptionIds.Count == 0)
        {
            return await ListAllAsync(cancellationToken);
        }

        var resolved = new List<SubscriptionInfo>();
        foreach (string subscriptionId in subscriptionIds)
        {
            using JsonDocument document = await armClient.GetJsonAsync(
                $"/subscriptions/{subscriptionId}?api-version=2022-12-01",
                cancellationToken);

            resolved.Add(ParseSubscription(document.RootElement));
        }

        EnsureSingleTenant(resolved);
        return resolved;
    }

    private async Task<IReadOnlyList<SubscriptionInfo>> ListAllAsync(CancellationToken cancellationToken)
    {
        var subscriptions = new List<SubscriptionInfo>();
        string? next = "/subscriptions?api-version=2022-12-01";

        while (!string.IsNullOrWhiteSpace(next))
        {
            using JsonDocument document = await armClient.GetJsonAsync(next, cancellationToken);
            JsonElement root = document.RootElement;
            if (root.TryGetProperty("value", out JsonElement value))
            {
                subscriptions.AddRange(value.EnumerateArray().Select(ParseSubscription));
            }

            next = root.TryGetProperty("nextLink", out JsonElement nextLink)
                ? nextLink.GetString()
                : null;
        }

        EnsureSingleTenant(subscriptions);
        return subscriptions;
    }

    private static SubscriptionInfo ParseSubscription(JsonElement element)
    {
        string id = GetString(element, "subscriptionId");
        string displayName = GetString(element, "displayName");
        string state = GetString(element, "state");
        string tenantId = GetString(element, "tenantId");
        return new SubscriptionInfo(id, displayName, state, tenantId);
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement value) ? value.GetString() ?? string.Empty : string.Empty;
    }

    private static void EnsureSingleTenant(IReadOnlyList<SubscriptionInfo> subscriptions)
    {
        string[] tenantIds = subscriptions
            .Select(subscription => subscription.TenantId)
            .Where(tenantId => !string.IsNullOrWhiteSpace(tenantId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (tenantIds.Length > 1)
        {
            throw new InvalidOperationException("Selected subscriptions span multiple tenants. Run the tool once per tenant.");
        }
    }
}
