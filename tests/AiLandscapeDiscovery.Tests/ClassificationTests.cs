using AiLandscapeDiscovery.Cli.Classification;
using AiLandscapeDiscovery.Cli.Discovery;

namespace AiLandscapeDiscovery.Tests;

public sealed class ClassificationTests
{
    private readonly AiResourceClassifier _classifier = new(AiResourceCatalog.Default);

    [Fact]
    public void ClassifyDetectsCognitiveServicesAsDirectAi()
    {
        var resource = Resource("Microsoft.CognitiveServices/accounts", skuName: "S0");

        ResourceClassification classification = _classifier.Classify(resource);

        Assert.Equal(ClassificationCategory.DirectAiResource, classification.Category);
    }

    [Fact]
    public void ClassifyDetectsGpuVirtualMachine()
    {
        var resource = Resource(
            "Microsoft.Compute/virtualMachines",
            propertiesJson: """{"hardwareProfile":{"vmSize":"Standard_NC24ads_A100_v4"}}""");

        ResourceClassification classification = _classifier.Classify(resource);

        Assert.Equal(ClassificationCategory.GpuCompute, classification.Category);
    }

    [Fact]
    public void InventoryBuilderIncludesDirectlyReferencedDependency()
    {
        var ai = Resource(
            "Microsoft.CognitiveServices/accounts",
            id: "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg/providers/Microsoft.CognitiveServices/accounts/ai",
            propertiesJson: """
            {"storage":"/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/aistore"}
            """);
        var storage = Resource(
            "Microsoft.Storage/storageAccounts",
            id: "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/aistore");

        Inventory inventory = InventoryBuilder.Build([], [ai, storage], _classifier);

        Assert.Contains(inventory.Resources, resource => resource.Id == storage.Id);
        Assert.Contains(inventory.Classifications, classification =>
            classification.ResourceId == storage.Id
            && classification.Category == ClassificationCategory.DirectlyRelatedDependency);
    }

    private static ResourceSnapshot Resource(
        string type,
        string id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg/providers/Microsoft.Mock/type/name",
        string skuName = "",
        string propertiesJson = "{}")
    {
        return new ResourceSnapshot(
            id,
            "name",
            type,
            "eastus",
            "rg",
            "00000000-0000-0000-0000-000000000000",
            "11111111-1111-1111-1111-111111111111",
            string.Empty,
            skuName,
            "{}",
            string.Empty,
            propertiesJson);
    }
}
