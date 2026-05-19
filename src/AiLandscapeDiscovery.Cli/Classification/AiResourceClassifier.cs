using AiLandscapeDiscovery.Cli.Discovery;

namespace AiLandscapeDiscovery.Cli.Classification;

public sealed class AiResourceClassifier(AiResourceCatalog catalog)
{
    public ResourceClassification Classify(ResourceSnapshot resource)
    {
        string type = Normalize(resource.Type);
        if (catalog.DirectResourceTypes.Contains(type)
            || catalog.DirectProviderPrefixes.Any(prefix => type.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return new ResourceClassification(
                resource.Id,
                ClassificationCategory.DirectAiResource,
                "Resource type is in the direct AI catalog.",
                resource.Type);
        }

        if (catalog.GpuResourceTypes.Contains(type) && ContainsGpuSku(resource))
        {
            return new ResourceClassification(
                resource.Id,
                ClassificationCategory.GpuCompute,
                "Resource is a GPU-capable compute type with an N-series GPU SKU signal.",
                resource.SkuName);
        }

        if (catalog.HostingResourceTypes.Contains(type) && ContainsHostingSignal(resource))
        {
            return new ResourceClassification(
                resource.Id,
                ClassificationCategory.InferredAiHosting,
                "Management-plane configuration contains an AI workload signal.",
                string.Join(';', catalog.HostingSignals.Where(signal => Contains(resource.PropertiesJson, signal))));
        }

        return ResourceClassification.NotAi(resource.Id);
    }

    private bool ContainsGpuSku(ResourceSnapshot resource)
    {
        string haystack = $"{resource.SkuName} {resource.PropertiesJson}".ToLowerInvariant();
        return catalog.GpuSkuPrefixes.Any(prefix => haystack.Contains(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private bool ContainsHostingSignal(ResourceSnapshot resource)
    {
        string haystack = $"{resource.Kind} {resource.TagsJson} {resource.PropertiesJson}";
        return catalog.HostingSignals.Any(signal => Contains(haystack, signal));
    }

    private static bool Contains(string value, string signal)
    {
        return value.Contains(signal, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string resourceType)
    {
        return resourceType.Trim().Trim('/').ToLowerInvariant();
    }
}

public sealed record ResourceClassification(
    string ResourceId,
    ClassificationCategory Category,
    string Reason,
    string Evidence)
{
    public static ResourceClassification NotAi(string resourceId)
    {
        return new ResourceClassification(resourceId, ClassificationCategory.NotAiRelated, string.Empty, string.Empty);
    }
}

public enum ClassificationCategory
{
    NotAiRelated,
    DirectAiResource,
    GpuCompute,
    InferredAiHosting,
    DirectlyRelatedDependency
}
