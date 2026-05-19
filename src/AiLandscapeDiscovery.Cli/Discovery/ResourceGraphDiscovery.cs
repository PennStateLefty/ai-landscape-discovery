using System.Text.Json;
using AiLandscapeDiscovery.Cli.Azure;

namespace AiLandscapeDiscovery.Cli.Discovery;

public sealed class ResourceGraphDiscovery(ArmRestClient armClient)
{
    private const string ResourceGraphPath = "/providers/Microsoft.ResourceGraph/resources?api-version=2022-10-01";
    private const int PageSize = 1000;
    private const int SubscriptionBatchSize = 100;
    private const string Query = """
Resources
| project id, name, type, location, resourceGroup, subscriptionId, kind, sku, tags, identity, properties
""";

    public async Task<IReadOnlyList<ResourceSnapshot>> DiscoverAsync(
        IReadOnlyList<string> subscriptionIds,
        CancellationToken cancellationToken)
    {
        var resources = new List<ResourceSnapshot>();
        foreach (string[] batch in subscriptionIds.Chunk(SubscriptionBatchSize))
        {
            resources.AddRange(await DiscoverBatchAsync(batch, cancellationToken));
        }

        return resources;
    }

    private async Task<IReadOnlyList<ResourceSnapshot>> DiscoverBatchAsync(
        IReadOnlyList<string> subscriptionIds,
        CancellationToken cancellationToken)
    {
        var resources = new List<ResourceSnapshot>();
        string? skipToken = null;

        do
        {
            var body = new Dictionary<string, object?>
            {
                ["subscriptions"] = subscriptionIds,
                ["query"] = Query,
                ["options"] = string.IsNullOrWhiteSpace(skipToken)
                    ? new Dictionary<string, object?> { ["$top"] = PageSize, ["resultFormat"] = "objectArray" }
                    : new Dictionary<string, object?> { ["$top"] = PageSize, ["$skipToken"] = skipToken, ["resultFormat"] = "objectArray" }
            };

            using JsonDocument document = await armClient.PostJsonAsync(ResourceGraphPath, body, cancellationToken);
            JsonElement root = document.RootElement;

            if (root.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Array)
            {
                resources.AddRange(data.EnumerateArray().Select(ResourceSnapshot.FromResourceGraphRow));
            }

            skipToken = ReadSkipToken(root);
        }
        while (!string.IsNullOrWhiteSpace(skipToken));

        return resources;
    }

    private static string? ReadSkipToken(JsonElement root)
    {
        if (root.TryGetProperty("$skipToken", out JsonElement dollarSkipToken))
        {
            return dollarSkipToken.GetString();
        }

        if (root.TryGetProperty("skipToken", out JsonElement skipToken))
        {
            return skipToken.GetString();
        }

        return null;
    }
}
