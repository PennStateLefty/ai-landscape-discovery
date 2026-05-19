# Architecture

## Design goals

AI Landscape Discovery produces a customer-readable inventory without requiring customer-side build tools. It prioritizes transparent source code, repeatable release artifacts, management-plane-only access, fail-fast authorization checks, and CSV files that work well in Excel or Power BI.

## Components

| Component | Responsibility |
| --- | --- |
| `Program` | Orchestrates option parsing, authentication, preflight, discovery, classification, and CSV writing. |
| `Auth` | Builds a customer-friendly Azure Identity credential chain. |
| `Azure` | Sends authenticated ARM requests through a Polly retry pipeline. |
| `Preflight` | Validates effective read access before high-volume discovery starts. |
| `Discovery` | Uses Azure Resource Graph through ARM to enumerate management-plane-visible resources. |
| `Classification` | Applies the AI resource catalog and GPU/hosting signals. |
| `Output` | Writes the normalized CSV bundle. |

## Data flow

1. Parse CLI options.
2. Authenticate using the selected authentication mode.
3. Resolve explicit subscriptions or enumerate visible subscriptions.
4. Run authorization preflight against each selected subscription.
5. Query Azure Resource Graph for management-plane-visible resources.
6. Explicitly enumerate management-plane child artifacts under Azure AI Services / Azure OpenAI / Foundry accounts and projects, including deployments, projects, RAI policies, connections, agents, tools, and models where ARM exposes them.
7. Classify direct AI resources, GPU compute, inferred AI hosting resources, and direct AI artifacts.
8. Include directly related adjacent resources only when explicit ARM-visible resource IDs are referenced.
9. Write CSV files and coverage-gap records.

## Authentication

The default `auto` mode uses Azure Identity's `DefaultAzureCredential`. Explicit modes are available for environments where a stale developer-tool token should not block sign-in:

- Service principal environment variables.
- Managed identity.
- Existing Azure CLI login.
- Existing Azure PowerShell login.
- Existing Azure Developer CLI login.
- Interactive browser login.
- Device code login.

Azure CLI is useful but not required. Use `--auth-mode device-code` for a standalone customer-friendly sign-in path. Device-code and browser modes persist both the token cache and an authentication record so repeated runs on the same machine can reuse the selected account.

## Resiliency

All ARM calls pass through a Polly retry pipeline. The pipeline retries transient HTTP failures, including 408, 429, and 5xx responses. It honors `Retry-After`, `x-ms-retry-after-ms`, and `retry-after-ms` headers when present, and otherwise uses exponential backoff with jitter.

## Trust boundary

The tool only calls Azure management-plane endpoints under `https://management.azure.com`. It does not call service data-plane APIs, does not mutate Azure resources, and writes discovery output only to the local output directory selected by the operator.
