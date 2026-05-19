# Output Schema

The tool writes a CSV bundle to the selected output directory.

| File | Purpose |
| --- | --- |
| `manifest.csv` | Run metadata, tenant, subscription count, resource count, and schema version. |
| `subscriptions.csv` | Subscriptions included in the run. |
| `resources.csv` | Canonical classified AI-related resources, AI artifacts, and directly related dependencies. It includes `tenantId` and `subscriptionId` so rows can be appended across subscription and tenant runs. |
| `ai_classifications.csv` | Classification category, reason, and evidence for each included resource. |
| `ai_artifacts.csv` | Derived index of AI deployments, models, agents, tools, and connections discovered under AI Services / Foundry resources. |
| `model_deployments.csv` | Management-plane-visible model and deployment resources. |
| `gpu_compute.csv` | GPU-capable compute resources detected by SKU signals. |
| `relationships.csv` | Explicit ARM-visible relationships between AI resources and dependencies. |
| `identities.csv` | Managed identity metadata for included resources. |
| `coverage_gaps.csv` | Known details skipped because v1 is management-plane only or requires additional permissions. |
| `errors.csv` | Discovery-stage errors that do not fail preflight. |
| `preflight_failures.csv` | Written only when authorization preflight fails. |
| `preflight_success.csv` | Written only for `--preflight-only` successful runs. |

## Canonical resource rows

`resources.csv` is the canonical data file. Other CSVs are run metadata, definitions, indexes, or diagnostic views derived from the same run.

Key `resources.csv` columns:

| Column | Meaning |
| --- | --- |
| `tenantId` | Azure tenant ID for append-safe cross-tenant analysis. |
| `subscriptionId` | Azure subscription ID for append-safe cross-subscription analysis. |
| `resourceId` | Full ARM resource ID. |
| `resourceGroup` | Resource group name. |
| `name` | ARM resource name. |
| `type` | ARM resource type. |
| `location` | Azure location. |
| `kind` | Provider-specific kind, when present. |
| `skuName` | SKU name, when present. |
| `tagsJson` | Raw tags JSON. |

## Classification categories

| Category | Meaning |
| --- | --- |
| `DirectAiResource` | Resource type is in the AI catalog. |
| `GpuCompute` | Compute resource has an N-series GPU SKU signal. |
| `InferredAiHosting` | Hosting resource has management-plane-visible AI workload signals. |
| `DirectlyRelatedDependency` | Adjacent resource is explicitly referenced by an AI-related resource. |
