# Operations and Troubleshooting

## Commands

Scan every visible subscription in the tenant:

```powershell
.\ai-landscape-discovery.exe scan --output .\snapshot
```

Scan explicit subscriptions:

```powershell
.\ai-landscape-discovery.exe scan --subscription <subscription-id> --subscription <subscription-id> --output .\snapshot
```

Use device-code authentication when local developer-tool tokens are expired or unavailable:

```powershell
.\ai-landscape-discovery.exe scan --subscription <subscription-id> --tenant-id <tenant-id> --auth-mode device-code --output .\snapshot
```

Run preflight only:

```powershell
.\ai-landscape-discovery.exe scan --preflight-only --output .\preflight
```

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Success. |
| `1` | General failure. |
| `2` | Authorization preflight failed. |
| `130` | Canceled. |

## Preflight failures

When preflight fails, review `preflight_failures.csv`. Assign Reader at the subscription scope or select a smaller set of subscriptions where the identity already has effective read access.

## Authentication modes

| Mode | Use when |
| --- | --- |
| `auto` | Default. Uses Azure Identity's default chain for service principals, managed identity, and existing developer-tool logins. |
| `azure-cli` | You specifically want to use the current Azure CLI login. |
| `device-code` | The customer cannot or should not depend on Azure CLI, or existing local tokens are expired. |
| `interactive-browser` | A browser-based sign-in is preferred. |

Device-code and interactive-browser modes use a persistent token cache named `ai-landscape-discovery` plus an authentication record stored under the user's home directory at `.ai-landscape-discovery/`. Repeated runs on the same machine should not prompt again until the cached token expires, is revoked, the authentication record is deleted, or a different tenant/account is selected.

## Throttling

The tool honors ARM retry headers and uses exponential backoff with jitter for transient failures. If a customer tenant is extremely large, run with explicit subscription batches.

## Data handling

Output is written only to the local output directory. The tool does not upload data elsewhere.
