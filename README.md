# AI Landscape Discovery

AI Landscape Discovery is a standalone .NET CLI that creates a point-in-time CSV snapshot of Azure AI-related resources across subscriptions in one Azure public-cloud tenant.

The tool is designed for customer trust: all source code, discovery logic, documentation, and release automation are visible in this repository. The customer handoff artifact is a self-contained Windows x64 executable, so customers do not need to install the .NET runtime or SDK.

## Quick start

```powershell
.\ai-landscape-discovery.exe scan --output .\snapshot
.\ai-landscape-discovery.exe scan --subscription <subscription-id> --output .\snapshot
.\ai-landscape-discovery.exe scan --subscription <subscription-id> --tenant-id <tenant-id> --auth-mode device-code --output .\snapshot
.\ai-landscape-discovery.exe scan --preflight-only --output .\preflight
```

The signed-in identity must have Reader, Contributor, Owner, or equivalent effective read access at the selected scope. Group-inherited access is valid.

## Scope

- Azure commercial/public cloud only.
- One tenant per run.
- Azure Resource Manager and management-plane APIs only.
- CSV output only.
- Direct AI resources, GPU compute, inferred AI hosting surfaces, and adjacent resources only when connected by explicit ARM-visible relationships.

## Documentation

- [Architecture](docs/architecture.md)
- [Discovery coverage](docs/discovery-coverage.md)
- [Least privilege](docs/least-privilege.md)
- [Output schema](docs/output-schema.md)
- [Operations and troubleshooting](docs/operations.md)
- [Release process](docs/release-process.md)
