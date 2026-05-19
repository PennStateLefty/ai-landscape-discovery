using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AiLandscapeDiscovery.Cli.Azure;

namespace AiLandscapeDiscovery.Cli.Discovery;

public sealed partial class AiServiceArtifactDiscovery(ArmRestClient armClient)
{
    private static readonly string[] AccountChildCollections =
    [
        "deployments",
        "projects",
        "raiPolicies"
    ];

    private static readonly string[] ProjectChildCollections =
    [
        "deployments",
        "connections",
        "agents",
        "tools",
        "models"
    ];

    private static readonly string[] ApiVersions =
    [
        "2025-06-01",
        "2024-10-01",
        "2023-05-01"
    ];

    public async Task<ArtifactDiscoveryResult> DiscoverAsync(
        IReadOnlyList<ResourceSnapshot> resources,
        CancellationToken cancellationToken)
    {
        var artifacts = new Dictionary<string, ResourceSnapshot>(StringComparer.OrdinalIgnoreCase);
        var coverageGaps = new List<CoverageGap>();
        var errors = new List<DiscoveryError>();

        foreach (ResourceSnapshot account in resources.Where(IsCognitiveServicesAccount))
        {
            foreach (string childCollection in AccountChildCollections)
            {
                await TryAddChildCollectionAsync(account, childCollection, artifacts, coverageGaps, errors, cancellationToken);
            }
        }

        IReadOnlyList<ResourceSnapshot> knownProjects = resources
            .Concat(artifacts.Values)
            .Where(IsCognitiveServicesProject)
            .ToArray();

        foreach (ResourceSnapshot project in knownProjects)
        {
            foreach (string childCollection in ProjectChildCollections)
            {
                await TryAddChildCollectionAsync(project, childCollection, artifacts, coverageGaps, errors, cancellationToken);
            }
        }

        return new ArtifactDiscoveryResult(artifacts.Values.ToArray(), coverageGaps, errors);
    }

    private async Task TryAddChildCollectionAsync(
        ResourceSnapshot parent,
        string childCollection,
        Dictionary<string, ResourceSnapshot> artifacts,
        List<CoverageGap> coverageGaps,
        List<DiscoveryError> errors,
        CancellationToken cancellationToken)
    {
        foreach (string apiVersion in ApiVersions)
        {
            try
            {
                IReadOnlyList<ResourceSnapshot> children = await ListChildResourcesAsync(parent, childCollection, apiVersion, cancellationToken);
                foreach (ResourceSnapshot child in children)
                {
                    artifacts[NormalizeId(child.Id)] = child;
                }

                return;
            }
            catch (ArmRequestException ex) when (IsUnsupportedApiShape(ex.StatusCode))
            {
                continue;
            }
            catch (ArmRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                errors.Add(new DiscoveryError(
                    parent.Id,
                    $"enrich:{childCollection}",
                    $"Forbidden while listing {childCollection}: {ex.Message}"));
                return;
            }
            catch (ArmRequestException ex)
            {
                errors.Add(new DiscoveryError(
                    parent.Id,
                    $"enrich:{childCollection}",
                    $"{(int)ex.StatusCode} while listing {childCollection}: {ex.Message}"));
                return;
            }
        }

        coverageGaps.Add(new CoverageGap(
            parent.Id,
            $"Azure AI {childCollection}",
            $"No supported management-plane API version returned {childCollection} under this resource.",
            "The artifact type may not exist for this account/project or may require service data-plane APIs."));
    }

    private async Task<IReadOnlyList<ResourceSnapshot>> ListChildResourcesAsync(
        ResourceSnapshot parent,
        string childCollection,
        string apiVersion,
        CancellationToken cancellationToken)
    {
        var children = new List<ResourceSnapshot>();
        string? next = $"{parent.Id.TrimEnd('/')}/{childCollection}?api-version={apiVersion}";

        while (!string.IsNullOrWhiteSpace(next))
        {
            using JsonDocument document = await armClient.GetJsonAsync(next, cancellationToken, retryServerErrors: false);
            JsonElement root = document.RootElement;
            if (root.TryGetProperty("value", out JsonElement values) && values.ValueKind == JsonValueKind.Array)
            {
                children.AddRange(values.EnumerateArray().Select(child =>
                    ResourceSnapshot.FromArmResource(
                        child,
                        parent.TenantId,
                        parent.SubscriptionId,
                        string.IsNullOrWhiteSpace(GetResourceGroup(child)) ? parent.ResourceGroup : GetResourceGroup(child))));
            }

            next = root.TryGetProperty("nextLink", out JsonElement nextLink)
                ? nextLink.GetString()
                : null;
        }

        return children;
    }

    private static bool IsUnsupportedApiShape(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.NotFound
            or HttpStatusCode.BadRequest
            or HttpStatusCode.MethodNotAllowed
            or HttpStatusCode.InternalServerError;
    }

    private static bool IsCognitiveServicesAccount(ResourceSnapshot resource)
    {
        return resource.Type.Equals("microsoft.cognitiveservices/accounts", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCognitiveServicesProject(ResourceSnapshot resource)
    {
        return resource.Type.Equals("microsoft.cognitiveservices/accounts/projects", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetResourceGroup(JsonElement resource)
    {
        string id = resource.TryGetProperty("id", out JsonElement idElement) ? idElement.GetString() ?? string.Empty : string.Empty;
        Match match = ResourceGroupRegex().Match(id);
        return match.Success ? match.Groups["resourceGroup"].Value : string.Empty;
    }

    private static string NormalizeId(string id)
    {
        return id.Trim().TrimEnd('/').ToLowerInvariant();
    }

    [GeneratedRegex("/resourceGroups/(?<resourceGroup>[^/]+)/providers/", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ResourceGroupRegex();
}

public sealed record ArtifactDiscoveryResult(
    IReadOnlyList<ResourceSnapshot> Artifacts,
    IReadOnlyList<CoverageGap> CoverageGaps,
    IReadOnlyList<DiscoveryError> Errors);
