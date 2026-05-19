namespace AiLandscapeDiscovery.Cli.Classification;

public sealed record AiResourceCatalog(
    IReadOnlySet<string> DirectResourceTypes,
    IReadOnlySet<string> DirectProviderPrefixes,
    IReadOnlySet<string> GpuResourceTypes,
    IReadOnlyList<string> GpuSkuPrefixes,
    IReadOnlySet<string> HostingResourceTypes,
    IReadOnlyList<string> HostingSignals)
{
    public static AiResourceCatalog Default { get; } = new(
        DirectResourceTypes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "microsoft.cognitiveservices/accounts",
            "microsoft.cognitiveservices/accounts/deployments",
            "microsoft.cognitiveservices/accounts/projects",
            "microsoft.cognitiveservices/accounts/projects/deployments",
            "microsoft.cognitiveservices/accounts/projects/connections",
            "microsoft.cognitiveservices/accounts/projects/agents",
            "microsoft.cognitiveservices/accounts/projects/tools",
            "microsoft.cognitiveservices/accounts/projects/models",
            "microsoft.cognitiveservices/accounts/raipolicies",
            "microsoft.search/searchservices",
            "microsoft.machinelearningservices/workspaces",
            "microsoft.machinelearningservices/workspaces/computes",
            "microsoft.machinelearningservices/workspaces/onlineendpoints",
            "microsoft.machinelearningservices/workspaces/onlineendpoints/deployments",
            "microsoft.machinelearningservices/workspaces/batchendpoints",
            "microsoft.machinelearningservices/workspaces/batchendpoints/deployments",
            "microsoft.machinelearningservices/workspaces/models",
            "microsoft.machinelearningservices/workspaces/environments",
            "microsoft.machinelearningservices/workspaces/components",
            "microsoft.machinelearningservices/registries",
            "microsoft.machinelearningservices/registries/models",
            "microsoft.machinelearningservices/registries/environments",
            "microsoft.machinelearningservices/registries/components",
            "microsoft.botservice/botservices",
            "microsoft.botservice/botservices/channels",
            "microsoft.videoindexer/accounts"
        },
        DirectProviderPrefixes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "microsoft.aifoundry/",
            "microsoft.agents/"
        },
        GpuResourceTypes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "microsoft.compute/virtualmachines",
            "microsoft.compute/virtualmachinescalesets",
            "microsoft.containerservice/managedclusters",
            "microsoft.containerservice/managedclusters/agentpools",
            "microsoft.batch/batchaccounts/pools",
            "microsoft.machinelearningservices/workspaces/computes"
        },
        GpuSkuPrefixes: ["standard_nc", "standard_nd", "standard_nv", "standard_ng"],
        HostingResourceTypes: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "microsoft.app/containerapps",
            "microsoft.app/managedenvironments",
            "microsoft.web/sites",
            "microsoft.web/serverfarms",
            "microsoft.containerservice/managedclusters"
        },
        HostingSignals:
        [
            "openai",
            "azureml",
            "huggingface",
            "llm",
            "vllm",
            "onnx",
            "tensorflow",
            "pytorch",
            "triton",
            "cuda",
            "nvidia"
        ]);
}
