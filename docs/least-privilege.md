# Least Privilege

## Required access

The signed-in identity must have Reader, Contributor, Owner, or equivalent effective read access at the selected top scope.

For v1, the selected top scope is each subscription being scanned. Group-inherited access counts as valid. Direct role assignment to the signed-in user or service principal is not required.

## Why preflight exists

The tool can make many ARM calls in large environments. Before discovery starts, it validates effective permissions and fails fast if access is insufficient. This avoids long partial scans that look complete but silently miss subscriptions or provider data.

## Recommended role

Use **Reader** at the subscription scope whenever possible. Reader is sufficient for broad management-plane resource inventory in most environments.

Contributor or Owner also pass preflight because they include effective read access, but they are not required for discovery and should not be granted solely for this tool.

## Example assignment

```bash
az role assignment create \
  --assignee <user-or-service-principal-object-id> \
  --role Reader \
  --scope /subscriptions/<subscription-id>
```

## Provider-specific limitations

Some provider-specific child resources may require additional read permissions or may not be visible through management-plane APIs. The tool reports these as preflight failures or coverage gaps instead of switching to data-plane access.
