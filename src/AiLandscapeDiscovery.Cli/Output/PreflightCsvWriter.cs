using AiLandscapeDiscovery.Cli.Preflight;

namespace AiLandscapeDiscovery.Cli.Output;

public static class PreflightCsvWriter
{
    public static Task WriteAsync(
        string path,
        IEnumerable<AuthorizationPreflightResult> results,
        CancellationToken cancellationToken)
    {
        string[] headers = ["subscriptionId", "subscriptionName", "tenantId", "scope", "succeeded", "message"];
        return CsvTableWriter.WriteAsync(
            path,
            headers,
            results.Select(result => new[]
            {
                result.SubscriptionId,
                result.SubscriptionName,
                result.TenantId,
                result.Scope,
                result.Succeeded.ToString(),
                result.Message
            }),
            cancellationToken);
    }
}
