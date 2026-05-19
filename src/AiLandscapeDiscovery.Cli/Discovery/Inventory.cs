using AiLandscapeDiscovery.Cli.Azure;
using AiLandscapeDiscovery.Cli.Classification;

namespace AiLandscapeDiscovery.Cli.Discovery;

public sealed record Inventory(
    string RunId,
    DateTimeOffset StartedAt,
    IReadOnlyList<SubscriptionInfo> Subscriptions,
    IReadOnlyList<ResourceSnapshot> Resources,
    IReadOnlyList<ResourceClassification> Classifications,
    IReadOnlyList<ResourceRelationship> Relationships,
    IReadOnlyList<AiArtifact> AiArtifacts,
    IReadOnlyList<CoverageGap> CoverageGaps,
    IReadOnlyList<DiscoveryError> Errors);

public sealed record ResourceRelationship(
    string SourceResourceId,
    string TargetResourceId,
    string RelationshipType,
    string Evidence);

public sealed record CoverageGap(
    string Scope,
    string Category,
    string Detail,
    string Reason);

public sealed record DiscoveryError(
    string Scope,
    string Stage,
    string Message);

public sealed record AiArtifact(
    string ResourceId,
    string TenantId,
    string SubscriptionId,
    string ResourceGroup,
    string ParentResourceId,
    string Name,
    string Type,
    string ArtifactKind,
    string Location,
    string PropertiesJson);
