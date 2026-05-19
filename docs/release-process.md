# Release Process

## GitHub workflow

The `build-release` workflow restores, builds, tests, publishes the self-contained Windows x64 executable, validates `--help`, packages the executable and docs, creates SHA-256 checksums, and uploads artifacts.

When the workflow runs for a tag named `v*`, it also creates a GitHub Release and attaches the release bundle.

## Manual release

1. Push all source and documentation changes.
2. Create and push a tag such as `v0.1.0`.
3. Let GitHub Actions build the release.
4. Review the generated GitHub Release, artifact checksum, and attached documentation.

## Local publish

```bash
dotnet publish src/AiLandscapeDiscovery.Cli/AiLandscapeDiscovery.Cli.csproj \
  --configuration Release \
  --runtime win-x64 \
  --output artifacts/win-x64
```

The Windows executable is `artifacts/win-x64/ai-landscape-discovery.exe`.
