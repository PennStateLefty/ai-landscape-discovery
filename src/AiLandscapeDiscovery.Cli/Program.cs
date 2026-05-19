using AiLandscapeDiscovery.Cli.Auth;
using AiLandscapeDiscovery.Cli.Azure;
using AiLandscapeDiscovery.Cli.Classification;
using AiLandscapeDiscovery.Cli.Discovery;
using AiLandscapeDiscovery.Cli.Output;
using AiLandscapeDiscovery.Cli.Preflight;
using AiLandscapeDiscovery.Cli.Runtime;

namespace AiLandscapeDiscovery.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        ParseResult parseResult = CliOptions.Parse(args);
        if (parseResult.ShouldExit)
        {
            if (!string.IsNullOrWhiteSpace(parseResult.Message))
            {
                Console.WriteLine(parseResult.Message);
            }

            return parseResult.ExitCode;
        }

        CliOptions options = parseResult.Options!;
        try
        {
            Directory.CreateDirectory(options.OutputDirectory);

            var credential = await AzureCredentialFactory.CreateAsync(options.TenantId, options.AuthMode, options.CancellationToken);
            var armClient = new ArmRestClient(credential, options.Verbose);
            var subscriptions = await new SubscriptionDiscovery(armClient)
                .ResolveAsync(options.SubscriptionIds, options.CancellationToken);

            if (subscriptions.Count == 0)
            {
                throw new InvalidOperationException("No subscriptions were visible to the signed-in identity.");
            }

            var preflightResults = await new AuthorizationPreflight(armClient)
                .ValidateAsync(subscriptions, options.CancellationToken);

            IReadOnlyList<AuthorizationPreflightResult> failures = preflightResults
                .Where(result => !result.Succeeded)
                .ToArray();

            if (failures.Count > 0)
            {
                string preflightPath = Path.Combine(options.OutputDirectory, "preflight_failures.csv");
                await PreflightCsvWriter.WriteAsync(preflightPath, failures, options.CancellationToken);
                Console.Error.WriteLine($"Authorization preflight failed. Details written to {preflightPath}");
                return 2;
            }

            if (options.PreflightOnly)
            {
                string preflightPath = Path.Combine(options.OutputDirectory, "preflight_success.csv");
                await PreflightCsvWriter.WriteAsync(preflightPath, preflightResults, options.CancellationToken);
                Console.WriteLine($"Authorization preflight succeeded. Details written to {preflightPath}");
                return 0;
            }

            var catalog = AiResourceCatalog.Default;
            var classifier = new AiResourceClassifier(catalog);
            var discovery = new ResourceGraphDiscovery(armClient);
            IReadOnlyList<ResourceSnapshot> resources = await discovery.DiscoverAsync(
                subscriptions.Select(subscription => subscription.SubscriptionId).ToArray(),
                options.CancellationToken);
            resources = ApplyTenantIds(resources, subscriptions);

            ArtifactDiscoveryResult artifactResult = await new AiServiceArtifactDiscovery(armClient)
                .DiscoverAsync(resources, options.CancellationToken);

            IReadOnlyList<ResourceSnapshot> enrichedResources = MergeResources(resources, artifactResult.Artifacts);

            Inventory inventory = InventoryBuilder.Build(
                subscriptions,
                enrichedResources,
                classifier,
                artifactResult.CoverageGaps,
                artifactResult.Errors);
            await InventoryCsvWriter.WriteAsync(options.OutputDirectory, inventory, options.CancellationToken);

            Console.WriteLine($"Discovery complete. CSV output written to {options.OutputDirectory}");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Discovery was canceled.");
            return 130;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(options.Verbose ? ex.ToString() : ex.Message);
            return 1;
        }
    }

    private static IReadOnlyList<ResourceSnapshot> ApplyTenantIds(
        IReadOnlyList<ResourceSnapshot> resources,
        IReadOnlyList<SubscriptionInfo> subscriptions)
    {
        Dictionary<string, string> tenantBySubscription = subscriptions.ToDictionary(
            subscription => subscription.SubscriptionId,
            subscription => subscription.TenantId,
            StringComparer.OrdinalIgnoreCase);

        return resources
            .Select(resource => string.IsNullOrWhiteSpace(resource.TenantId)
                && tenantBySubscription.TryGetValue(resource.SubscriptionId, out string? tenantId)
                    ? resource.WithTenantId(tenantId)
                    : resource)
            .ToArray();
    }

    private static IReadOnlyList<ResourceSnapshot> MergeResources(
        IReadOnlyList<ResourceSnapshot> baseResources,
        IReadOnlyList<ResourceSnapshot> enrichedResources)
    {
        var merged = new Dictionary<string, ResourceSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (ResourceSnapshot resource in baseResources.Concat(enrichedResources))
        {
            merged[resource.Id.Trim().TrimEnd('/')] = resource;
        }

        return merged.Values.ToArray();
    }
}
