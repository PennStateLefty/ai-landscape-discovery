using System.Text.RegularExpressions;
using AiLandscapeDiscovery.Cli.Azure;
using AiLandscapeDiscovery.Cli.Classification;

namespace AiLandscapeDiscovery.Cli.Discovery;

public static partial class InventoryBuilder
{
    public static Inventory Build(
        IReadOnlyList<SubscriptionInfo> subscriptions,
        IReadOnlyList<ResourceSnapshot> allResources,
        AiResourceClassifier classifier,
        IReadOnlyList<CoverageGap>? enrichmentCoverageGaps = null,
        IReadOnlyList<DiscoveryError>? enrichmentErrors = null)
    {
        var resourcesById = allResources.ToDictionary(resource => NormalizeId(resource.Id), StringComparer.OrdinalIgnoreCase);
        var includedResources = new Dictionary<string, ResourceSnapshot>(StringComparer.OrdinalIgnoreCase);
        var classifications = new Dictionary<string, ResourceClassification>(StringComparer.OrdinalIgnoreCase);
        var relationships = new List<ResourceRelationship>();
        var coverageGaps = new List<CoverageGap>();
        var errors = new List<DiscoveryError>();
        if (enrichmentCoverageGaps is not null)
        {
            coverageGaps.AddRange(enrichmentCoverageGaps);
        }

        if (enrichmentErrors is not null)
        {
            errors.AddRange(enrichmentErrors);
        }

        foreach (ResourceSnapshot resource in allResources)
        {
            ResourceClassification classification = classifier.Classify(resource);
            if (classification.Category == ClassificationCategory.NotAiRelated)
            {
                continue;
            }

            includedResources[NormalizeId(resource.Id)] = resource;
            classifications[NormalizeId(resource.Id)] = classification;
            AddCoverageGaps(resource, coverageGaps);
        }

        foreach (ResourceSnapshot source in includedResources.Values.ToArray())
        {
            foreach (string referencedId in ExtractResourceIds(source.PropertiesJson))
            {
                string normalizedReference = NormalizeId(referencedId);
                if (!resourcesById.TryGetValue(normalizedReference, out ResourceSnapshot? target))
                {
                    continue;
                }

                if (NormalizeId(target.Id).Equals(NormalizeId(source.Id), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                includedResources.TryAdd(normalizedReference, target);
                classifications.TryAdd(
                    normalizedReference,
                    new ResourceClassification(
                        target.Id,
                        ClassificationCategory.DirectlyRelatedDependency,
                        "Resource is explicitly referenced by an AI-related resource through management-plane properties.",
                        source.Id));

                relationships.Add(new ResourceRelationship(
                    source.Id,
                    target.Id,
                    "explicitArmReference",
                    "Target resource ID appeared in source resource properties."));
            }
        }

        return new Inventory(
            RunId: Guid.NewGuid().ToString("n"),
            StartedAt: DateTimeOffset.UtcNow,
            Subscriptions: subscriptions,
            Resources: includedResources.Values.OrderBy(resource => resource.Type).ThenBy(resource => resource.Name).ToArray(),
            Classifications: classifications.Values.OrderBy(classification => classification.ResourceId).ToArray(),
            Relationships: relationships.Distinct().OrderBy(relationship => relationship.SourceResourceId).ToArray(),
            AiArtifacts: BuildArtifacts(includedResources.Values).OrderBy(artifact => artifact.Type).ThenBy(artifact => artifact.Name).ToArray(),
            CoverageGaps: coverageGaps.Distinct().OrderBy(gap => gap.Scope).ThenBy(gap => gap.Category).ToArray(),
            Errors: errors.Distinct().OrderBy(error => error.Scope).ThenBy(error => error.Stage).ToArray());
    }

    private static IEnumerable<AiArtifact> BuildArtifacts(IEnumerable<ResourceSnapshot> resources)
    {
        foreach (ResourceSnapshot resource in resources)
        {
            string artifactKind = GetArtifactKind(resource.Type);
            if (string.IsNullOrWhiteSpace(artifactKind))
            {
                continue;
            }

            yield return new AiArtifact(
                resource.Id,
                resource.TenantId,
                resource.SubscriptionId,
                resource.ResourceGroup,
                GetParentResourceId(resource.Id),
                resource.Name,
                resource.Type,
                artifactKind,
                resource.Location,
                resource.PropertiesJson);
        }
    }

    private static string GetArtifactKind(string resourceType)
    {
        string normalized = resourceType.ToLowerInvariant();
        if (normalized.EndsWith("/deployments", StringComparison.OrdinalIgnoreCase))
        {
            return "deployment";
        }

        if (normalized.EndsWith("/models", StringComparison.OrdinalIgnoreCase))
        {
            return "model";
        }

        if (normalized.EndsWith("/agents", StringComparison.OrdinalIgnoreCase))
        {
            return "agent";
        }

        if (normalized.EndsWith("/tools", StringComparison.OrdinalIgnoreCase))
        {
            return "tool";
        }

        if (normalized.EndsWith("/connections", StringComparison.OrdinalIgnoreCase))
        {
            return "connection";
        }

        return string.Empty;
    }

    private static string GetParentResourceId(string resourceId)
    {
        string[] segments = resourceId.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return string.Empty;
        }

        return "/" + string.Join('/', segments.Take(segments.Length - 2));
    }

    private static void AddCoverageGaps(ResourceSnapshot resource, List<CoverageGap> coverageGaps)
    {
        string type = resource.Type.ToLowerInvariant();
        if (type.Equals("microsoft.search/searchservices", StringComparison.OrdinalIgnoreCase))
        {
            coverageGaps.Add(new CoverageGap(
                resource.Id,
                "Azure AI Search internals",
                "Indexes, indexers, skillsets, datasources, semantic configurations, and vectorizers may require service data-plane APIs.",
                "v1 is restricted to ARM and management-plane APIs."));
        }

        if (type.StartsWith("microsoft.cognitiveservices/accounts", StringComparison.OrdinalIgnoreCase))
        {
            coverageGaps.Add(new CoverageGap(
                resource.Id,
                "Azure AI service model metadata",
                "Some model, deployment, quota, and policy metadata may not be exposed through generic ARM resource enumeration.",
                "v1 records only management-plane-visible fields."));
        }
    }

    private static IEnumerable<string> ExtractResourceIds(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            yield break;
        }

        foreach (Match match in ResourceIdRegex().Matches(json))
        {
            yield return match.Value.TrimEnd('"', '\'', ',', ';', ')', ']', '}');
        }
    }

    private static string NormalizeId(string id)
    {
        return id.Trim().TrimEnd('/').ToLowerInvariant();
    }

    [GeneratedRegex("/subscriptions/[0-9a-fA-F-]+/resourceGroups/[^\"'\\s,;]+/providers/[^\"'\\s,;]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ResourceIdRegex();
}
