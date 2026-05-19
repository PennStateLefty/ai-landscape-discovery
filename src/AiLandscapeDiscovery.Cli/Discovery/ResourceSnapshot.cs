using System.Text.Json;

namespace AiLandscapeDiscovery.Cli.Discovery;

public sealed record ResourceSnapshot(
    string Id,
    string Name,
    string Type,
    string Location,
    string ResourceGroup,
    string SubscriptionId,
    string TenantId,
    string Kind,
    string SkuName,
    string TagsJson,
    string IdentityJson,
    string PropertiesJson)
{
    public static ResourceSnapshot FromResourceGraphRow(JsonElement row)
    {
        return new ResourceSnapshot(
            GetString(row, "id"),
            GetString(row, "name"),
            GetString(row, "type"),
            GetString(row, "location"),
            GetString(row, "resourceGroup"),
            GetString(row, "subscriptionId"),
            GetString(row, "tenantId"),
            GetString(row, "kind"),
            ReadSkuName(row),
            GetRawJson(row, "tags"),
            GetRawJson(row, "identity"),
            GetRawJson(row, "properties"));
    }

    public static ResourceSnapshot FromArmResource(JsonElement resource, string tenantId, string subscriptionId, string resourceGroup)
    {
        return new ResourceSnapshot(
            GetString(resource, "id"),
            GetString(resource, "name"),
            GetString(resource, "type"),
            GetString(resource, "location"),
            resourceGroup,
            subscriptionId,
            tenantId,
            GetString(resource, "kind"),
            ReadSkuName(resource),
            GetRawJson(resource, "tags"),
            GetRawJson(resource, "identity"),
            GetRawJson(resource, "properties"));
    }

    public ResourceSnapshot WithTenantId(string tenantId)
    {
        return this with { TenantId = tenantId };
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string GetRawJson(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind != JsonValueKind.Null
            ? value.GetRawText()
            : string.Empty;
    }

    private static string ReadSkuName(JsonElement element)
    {
        if (!element.TryGetProperty("sku", out JsonElement sku) || sku.ValueKind == JsonValueKind.Null)
        {
            return string.Empty;
        }

        if (sku.ValueKind == JsonValueKind.String)
        {
            return sku.GetString() ?? string.Empty;
        }

        return sku.TryGetProperty("name", out JsonElement name) ? name.GetString() ?? string.Empty : sku.GetRawText();
    }
}
