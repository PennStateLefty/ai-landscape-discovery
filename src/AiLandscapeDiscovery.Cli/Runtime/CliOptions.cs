using System.Reflection;
using AiLandscapeDiscovery.Cli.Auth;

namespace AiLandscapeDiscovery.Cli.Runtime;

public sealed record CliOptions(
    IReadOnlyList<string> SubscriptionIds,
    string OutputDirectory,
    string? TenantId,
    AuthMode AuthMode,
    int Concurrency,
    bool PreflightOnly,
    bool Verbose,
    CancellationToken CancellationToken)
{
    public static ParseResult Parse(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("scan", StringComparison.OrdinalIgnoreCase))
        {
            args = args[1..];
        }

        var subscriptions = new List<string>();
        string? outputDirectory = null;
        string? tenantId = null;
        AuthMode authMode = AuthMode.Auto;
        int concurrency = 4;
        bool preflightOnly = false;
        bool verbose = false;

        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg)
                {
                    case "-h":
                    case "--help":
                        return ParseResult.Exit(0, HelpText);
                    case "--version":
                        return ParseResult.Exit(0, GetVersion());
                    case "-s":
                    case "--subscription":
                        subscriptions.AddRange(ReadValue(args, ref i, arg).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        break;
                    case "-o":
                    case "--output":
                        outputDirectory = ReadValue(args, ref i, arg);
                        break;
                    case "--tenant-id":
                        tenantId = ReadValue(args, ref i, arg);
                        break;
                    case "--auth-mode":
                        if (!TryParseAuthMode(ReadValue(args, ref i, arg), out authMode))
                        {
                            return ParseResult.Exit(1, "--auth-mode must be one of: auto, azure-cli, device-code, interactive-browser.");
                        }

                        break;
                    case "--concurrency":
                        if (!int.TryParse(ReadValue(args, ref i, arg), out concurrency) || concurrency < 1 || concurrency > 32)
                        {
                            return ParseResult.Exit(1, "--concurrency must be an integer between 1 and 32.");
                        }

                        break;
                    case "--preflight-only":
                        preflightOnly = true;
                        break;
                    case "-v":
                    case "--verbose":
                        verbose = true;
                        break;
                    default:
                        if (arg.StartsWith("-", StringComparison.Ordinal))
                        {
                            return ParseResult.Exit(1, $"Unknown option '{arg}'. Run with --help for usage.");
                        }

                        return ParseResult.Exit(1, $"Unexpected argument '{arg}'. Run with --help for usage.");
                }
            }
        }
        catch (ArgumentException ex)
        {
            return ParseResult.Exit(1, ex.Message);
        }

        outputDirectory ??= Path.Combine(Environment.CurrentDirectory, $"ai-landscape-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}");
        return ParseResult.Success(new CliOptions(
            subscriptions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Path.GetFullPath(outputDirectory),
            string.IsNullOrWhiteSpace(tenantId) ? null : tenantId,
            authMode,
            concurrency,
            preflightOnly,
            verbose,
            CancellationToken.None));
    }

    private static string ReadValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("-", StringComparison.Ordinal))
        {
            throw new ArgumentException($"{option} requires a value.");
        }

        index++;
        return args[index];
    }

    private static bool TryParseAuthMode(string value, out AuthMode authMode)
    {
        authMode = value.ToLowerInvariant() switch
        {
            "auto" => AuthMode.Auto,
            "azure-cli" => AuthMode.AzureCli,
            "device-code" => AuthMode.DeviceCode,
            "interactive-browser" => AuthMode.InteractiveBrowser,
            _ => AuthMode.Auto
        };

        return value.Equals("auto", StringComparison.OrdinalIgnoreCase)
            || value.Equals("azure-cli", StringComparison.OrdinalIgnoreCase)
            || value.Equals("device-code", StringComparison.OrdinalIgnoreCase)
            || value.Equals("interactive-browser", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetVersion()
    {
        Assembly assembly = typeof(CliOptions).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }

    private const string HelpText = """
AI Landscape Discovery

Usage:
  ai-landscape-discovery scan [options]

Options:
  -s, --subscription <id>   Subscription ID to scan. Repeat or comma-separate for multiple subscriptions.
                            If omitted, all visible subscriptions in the current tenant are scanned.
  -o, --output <path>       Output directory for CSV files. Defaults to ./ai-landscape-<timestamp>.
      --tenant-id <id>      Tenant ID hint for interactive and command-line credentials.
      --auth-mode <mode>    Authentication mode: auto, azure-cli, device-code, interactive-browser.
                            Defaults to auto. Use device-code when local CLI tokens are expired.
      --concurrency <n>     Reserved for provider enrichment concurrency. Defaults to 4.
      --preflight-only      Validate access and write preflight CSV without resource discovery.
  -v, --verbose             Print detailed errors.
      --version             Print version.
  -h, --help                Show help.
""";
}

public sealed record ParseResult(CliOptions? Options, bool ShouldExit, int ExitCode, string? Message)
{
    public static ParseResult Success(CliOptions options) => new(options, false, 0, null);

    public static ParseResult Exit(int exitCode, string message) => new(null, true, exitCode, message);
}
