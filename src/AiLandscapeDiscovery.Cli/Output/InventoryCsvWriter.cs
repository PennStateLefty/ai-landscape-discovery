using AiLandscapeDiscovery.Cli.Classification;
using AiLandscapeDiscovery.Cli.Discovery;

namespace AiLandscapeDiscovery.Cli.Output;

public static class InventoryCsvWriter
{
    public static async Task WriteAsync(string outputDirectory, Inventory inventory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        await WriteManifestAsync(outputDirectory, inventory, cancellationToken);
        await WriteSubscriptionsAsync(outputDirectory, inventory, cancellationToken);
        await WriteResourcesAsync(outputDirectory, inventory, cancellationToken);
        await WriteClassificationsAsync(outputDirectory, inventory, cancellationToken);
        await WriteAiArtifactsAsync(outputDirectory, inventory, cancellationToken);
        await WriteModelDeploymentsAsync(outputDirectory, inventory, cancellationToken);
        await WriteGpuComputeAsync(outputDirectory, inventory, cancellationToken);
        await WriteRelationshipsAsync(outputDirectory, inventory, cancellationToken);
        await WriteIdentitiesAsync(outputDirectory, inventory, cancellationToken);
        await WriteCoverageGapsAsync(outputDirectory, inventory, cancellationToken);
        await WriteErrorsAsync(outputDirectory, inventory, cancellationToken);
    }

    private static Task WriteManifestAsync(string outputDirectory, Inventory inventory, CancellationToken cancellationToken)
    {
        string[] headers = ["runId", "startedAtUtc", "tenantId", "subscriptionCount", "resourceCount", "schemaVersion"];
        string tenantId = inventory.Subscriptions.Select(subscription => subscription.TenantId).FirstOrDefault(id => !string.IsNullOrWhiteSpace(id)) ?? string.Empty;
        return CsvTableWriter.WriteAsync(
            Path.Combine(outputDirectory, "manifest.csv"),
            headers,
            [new[] { inventory.RunId, inventory.StartedAt.UtcDateTime.ToString("O"), tenantId, inventory.Subscriptions.Count.ToString(), inventory.Resources.Count.ToString(), "1" }],
            cancellationToken);
    }

    private static Task WriteSubscriptionsAsync(string outputDirectory, Inventory inventory, CancellationToken cancellationToken)
    {
        string[] headers = ["subscriptionId", "displayName", "state", "tenantId"];
        return CsvTableWriter.WriteAsync(
            Path.Combine(outputDirectory, "subscriptions.csv"),
            headers,
            inventory.Subscriptions.Select(subscription => new[]
            {
                subscription.SubscriptionId,
                subscription.DisplayName,
                subscription.State,
                subscription.TenantId
            }),
            cancellationToken);
    }

    private static Task WriteResourcesAsync(string outputDirectory, Inventory inventory, CancellationToken cancellationToken)
    {
        string[] headers =
        [
            "tenantId",
            "subscriptionId",
            "resourceId",
            "resourceGroup",
            "name",
            "type",
            "location",
            "kind",
            "skuName",
            "tagsJson"
        ];

        return CsvTableWriter.WriteAsync(
            Path.Combine(outputDirectory, "resources.csv"),
            headers,
            inventory.Resources.Select(resource => new[]
            {
                resource.TenantId,
                resource.SubscriptionId,
                resource.Id,
                resource.ResourceGroup,
                resource.Name,
                resource.Type,
                resource.Location,
                resource.Kind,
                resource.SkuName,
                resource.TagsJson
            }),
            cancellationToken);
    }

    private static Task WriteClassificationsAsync(string outputDirectory, Inventory inventory, CancellationToken cancellationToken)
    {
        string[] headers = ["resourceId", "category", "reason", "evidence"];
        return CsvTableWriter.WriteAsync(
            Path.Combine(outputDirectory, "ai_classifications.csv"),
            headers,
            inventory.Classifications.Select(classification => new[]
            {
                classification.ResourceId,
                classification.Category.ToString(),
                classification.Reason,
                classification.Evidence
            }),
            cancellationToken);
    }

    private static Task WriteModelDeploymentsAsync(string outputDirectory, Inventory inventory, CancellationToken cancellationToken)
    {
        string[] headers = ["tenantId", "subscriptionId", "resourceGroup", "resourceId", "parentResourceId", "name", "type", "artifactKind", "location", "propertiesJson"];
        return CsvTableWriter.WriteAsync(
            Path.Combine(outputDirectory, "model_deployments.csv"),
            headers,
            inventory.AiArtifacts
                .Where(artifact => artifact.ArtifactKind is "deployment" or "model")
                .Select(artifact => new[]
                {
                    artifact.TenantId,
                    artifact.SubscriptionId,
                    artifact.ResourceGroup,
                    artifact.ResourceId,
                    artifact.ParentResourceId,
                    artifact.Name,
                    artifact.Type,
                    artifact.ArtifactKind,
                    artifact.Location,
                    artifact.PropertiesJson
                }),
            cancellationToken);
    }

    private static Task WriteAiArtifactsAsync(string outputDirectory, Inventory inventory, CancellationToken cancellationToken)
    {
        string[] headers = ["tenantId", "subscriptionId", "resourceGroup", "resourceId", "parentResourceId", "name", "type", "artifactKind", "location", "propertiesJson"];
        return CsvTableWriter.WriteAsync(
            Path.Combine(outputDirectory, "ai_artifacts.csv"),
            headers,
            inventory.AiArtifacts.Select(artifact => new[]
            {
                artifact.TenantId,
                artifact.SubscriptionId,
                artifact.ResourceGroup,
                artifact.ResourceId,
                artifact.ParentResourceId,
                artifact.Name,
                artifact.Type,
                artifact.ArtifactKind,
                artifact.Location,
                artifact.PropertiesJson
            }),
            cancellationToken);
    }

    private static Task WriteGpuComputeAsync(string outputDirectory, Inventory inventory, CancellationToken cancellationToken)
    {
        HashSet<string> gpuIds = inventory.Classifications
            .Where(classification => classification.Category == ClassificationCategory.GpuCompute)
            .Select(classification => classification.ResourceId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] headers = ["tenantId", "subscriptionId", "resourceGroup", "resourceId", "name", "type", "location", "skuName", "propertiesJson"];
        return CsvTableWriter.WriteAsync(
            Path.Combine(outputDirectory, "gpu_compute.csv"),
            headers,
            inventory.Resources
                .Where(resource => gpuIds.Contains(resource.Id))
                .Select(resource => new[]
                {
                    resource.TenantId,
                    resource.SubscriptionId,
                    resource.ResourceGroup,
                    resource.Id,
                    resource.Name,
                    resource.Type,
                    resource.Location,
                    resource.SkuName,
                    resource.PropertiesJson
                }),
            cancellationToken);
    }

    private static Task WriteRelationshipsAsync(string outputDirectory, Inventory inventory, CancellationToken cancellationToken)
    {
        string[] headers = ["sourceResourceId", "targetResourceId", "relationshipType", "evidence"];
        return CsvTableWriter.WriteAsync(
            Path.Combine(outputDirectory, "relationships.csv"),
            headers,
            inventory.Relationships.Select(relationship => new[]
            {
                relationship.SourceResourceId,
                relationship.TargetResourceId,
                relationship.RelationshipType,
                relationship.Evidence
            }),
            cancellationToken);
    }

    private static Task WriteIdentitiesAsync(string outputDirectory, Inventory inventory, CancellationToken cancellationToken)
    {
        string[] headers = ["tenantId", "subscriptionId", "resourceId", "identityJson"];
        return CsvTableWriter.WriteAsync(
            Path.Combine(outputDirectory, "identities.csv"),
            headers,
            inventory.Resources
                .Where(resource => !string.IsNullOrWhiteSpace(resource.IdentityJson))
                .Select(resource => new[] { resource.TenantId, resource.SubscriptionId, resource.Id, resource.IdentityJson }),
            cancellationToken);
    }

    private static Task WriteCoverageGapsAsync(string outputDirectory, Inventory inventory, CancellationToken cancellationToken)
    {
        string[] headers = ["scope", "category", "detail", "reason"];
        return CsvTableWriter.WriteAsync(
            Path.Combine(outputDirectory, "coverage_gaps.csv"),
            headers,
            inventory.CoverageGaps.Select(gap => new[] { gap.Scope, gap.Category, gap.Detail, gap.Reason }),
            cancellationToken);
    }

    private static Task WriteErrorsAsync(string outputDirectory, Inventory inventory, CancellationToken cancellationToken)
    {
        string[] headers = ["scope", "stage", "message"];
        return CsvTableWriter.WriteAsync(
            Path.Combine(outputDirectory, "errors.csv"),
            headers,
            inventory.Errors.Select(error => new[] { error.Scope, error.Stage, error.Message }),
            cancellationToken);
    }
}
