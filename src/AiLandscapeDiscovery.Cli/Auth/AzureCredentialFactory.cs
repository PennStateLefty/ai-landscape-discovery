using Azure.Core;
using Azure.Identity;

namespace AiLandscapeDiscovery.Cli.Auth;

public static class AzureCredentialFactory
{
    private const string TokenCacheName = "ai-landscape-discovery";
    private static readonly string[] ArmScopes = ["https://management.azure.com/.default"];

    public static async Task<TokenCredential> CreateAsync(string? tenantId, AuthMode authMode, CancellationToken cancellationToken)
    {
        return authMode switch
        {
            AuthMode.Auto => new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                TenantId = tenantId,
                ExcludeInteractiveBrowserCredential = true
            }),
            AuthMode.AzureCli => new AzureCliCredential(new AzureCliCredentialOptions { TenantId = tenantId }),
            AuthMode.DeviceCode => await CreateDeviceCodeCredentialAsync(tenantId, cancellationToken),
            AuthMode.InteractiveBrowser => await CreateInteractiveBrowserCredentialAsync(tenantId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(authMode), authMode, "Unsupported authentication mode.")
        };
    }

    private static async Task<TokenCredential> CreateDeviceCodeCredentialAsync(string? tenantId, CancellationToken cancellationToken)
    {
        string authRecordPath = GetAuthenticationRecordPath(tenantId, AuthMode.DeviceCode);
        AuthenticationRecord? authenticationRecord = await ReadAuthenticationRecordAsync(authRecordPath, cancellationToken);
        var credential = new DeviceCodeCredential(new DeviceCodeCredentialOptions
        {
            TenantId = tenantId,
            AuthenticationRecord = authenticationRecord,
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions { Name = TokenCacheName },
            DeviceCodeCallback = static (info, _) =>
            {
                Console.WriteLine(info.Message);
                return Task.CompletedTask;
            }
        });

        if (authenticationRecord is null)
        {
            authenticationRecord = await credential.AuthenticateAsync(new TokenRequestContext(ArmScopes), cancellationToken);
            await WriteAuthenticationRecordAsync(authRecordPath, authenticationRecord, cancellationToken);
            credential = new DeviceCodeCredential(new DeviceCodeCredentialOptions
            {
                TenantId = tenantId,
                AuthenticationRecord = authenticationRecord,
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions { Name = TokenCacheName },
                DeviceCodeCallback = static (info, _) =>
                {
                    Console.WriteLine(info.Message);
                    return Task.CompletedTask;
                }
            });
        }

        return credential;
    }

    private static async Task<TokenCredential> CreateInteractiveBrowserCredentialAsync(string? tenantId, CancellationToken cancellationToken)
    {
        string authRecordPath = GetAuthenticationRecordPath(tenantId, AuthMode.InteractiveBrowser);
        AuthenticationRecord? authenticationRecord = await ReadAuthenticationRecordAsync(authRecordPath, cancellationToken);
        var credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
        {
            TenantId = tenantId,
            AuthenticationRecord = authenticationRecord,
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions { Name = TokenCacheName }
        });

        if (authenticationRecord is null)
        {
            authenticationRecord = await credential.AuthenticateAsync(new TokenRequestContext(ArmScopes), cancellationToken);
            await WriteAuthenticationRecordAsync(authRecordPath, authenticationRecord, cancellationToken);
            credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            {
                TenantId = tenantId,
                AuthenticationRecord = authenticationRecord,
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions { Name = TokenCacheName }
            });
        }

        return credential;
    }

    private static async Task<AuthenticationRecord?> ReadAuthenticationRecordAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        await using FileStream stream = File.OpenRead(path);
        return await AuthenticationRecord.DeserializeAsync(stream, cancellationToken);
    }

    private static async Task WriteAuthenticationRecordAsync(
        string path,
        AuthenticationRecord authenticationRecord,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        await using FileStream stream = File.Create(path);
        await authenticationRecord.SerializeAsync(stream, cancellationToken);
    }

    private static string GetAuthenticationRecordPath(string? tenantId, AuthMode authMode)
    {
        string safeTenant = string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId;
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ai-landscape-discovery");
        return Path.Combine(directory, $"{authMode.ToString().ToLowerInvariant()}-{safeTenant}.authrecord");
    }
}

public enum AuthMode
{
    Auto,
    AzureCli,
    DeviceCode,
    InteractiveBrowser
}
