# Discovery Coverage

## v1 boundary

v1 is management-plane only. If a service detail requires a data-plane API, the tool records a coverage gap instead of calling that API.

## Direct AI resource catalog

The initial catalog includes these management-plane resource families:

| Area | Resource types and signals |
| --- | --- |
| Azure OpenAI and Azure AI services | `Microsoft.CognitiveServices/accounts`, deployments, projects, responsible AI policy child resources, project deployments, project connections/tools, agents, and models where ARM exposes them. |
| Azure AI Foundry / AI Studio | Foundry-style resources exposed through `Microsoft.CognitiveServices`, `Microsoft.MachineLearningServices`, or `Microsoft.AIFoundry` provider namespaces. |
| Azure Machine Learning | Workspaces, computes, endpoints, endpoint deployments, registries, models, environments, and components where ARM exposes them. |
| Azure AI Search | Search services and management-plane-visible metadata. |
| Bot and conversational AI | `Microsoft.BotService` resources. |
| Video Indexer | `Microsoft.VideoIndexer/accounts`. |
| GPU compute | VM, VMSS, AKS, Batch, and AML compute resources with N-series SKU signals. |
| AI hosting signals | Container Apps, App Service, AKS, and related hosting surfaces with management-plane-visible AI workload signals. |

## Adjacent resources

v1 does not inventory every storage account, key vault, network, database, or messaging service in the subscription. Adjacent resources are included only when an AI-related resource explicitly references them through ARM-visible relationships such as:

- Resource ID references in properties.
- Managed resource groups.
- Managed identities.
- Private endpoint connections.
- Diagnostic settings.
- Parent/child resource relationships.

## Known coverage gaps

Some service internals are intentionally out of scope for v1 because they commonly require data-plane APIs:

- Azure AI Search indexes, indexers, skillsets, datasources, semantic configurations, and vectorizers.
- Some model metadata, quota metadata, and policy details for Azure OpenAI / Azure AI services.
- Runtime details inside containers or Kubernetes workloads that are not visible through ARM.

These gaps are written to `coverage_gaps.csv`.

## Artifact enrichment

After broad Resource Graph inventory, the tool iterates each `Microsoft.CognitiveServices/accounts` resource and each discovered `Microsoft.CognitiveServices/accounts/projects` resource through ARM child collection APIs. The enrichment pass attempts supported management-plane API versions for:

- Account deployments.
- Account projects.
- Account RAI policies.
- Project deployments.
- Project connections.
- Project agents.
- Project tools.
- Project models.

Discovered artifacts are added to `resources.csv` as normal resource rows and summarized in `ai_artifacts.csv`; deployment/model artifacts are also summarized in `model_deployments.csv`.
