using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Polly;
using Polly.Retry;

namespace AiLandscapeDiscovery.Cli.Azure;

public sealed class ArmRestClient
{
    private static readonly string[] ArmScopes = ["https://management.azure.com/.default"];
    private readonly TokenCredential _credential;
    private readonly HttpClient _httpClient;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;
    private readonly ResiliencePipeline<HttpResponseMessage> _throttlingPipeline;
    private readonly bool _verbose;

    public ArmRestClient(TokenCredential credential, bool verbose, HttpClient? httpClient = null)
    {
        _credential = credential;
        _verbose = verbose;
        _httpClient = httpClient ?? new HttpClient { BaseAddress = new Uri("https://management.azure.com") };
        _pipeline = BuildPipeline(retryServerErrors: true);
        _throttlingPipeline = BuildPipeline(retryServerErrors: false);
    }

    public async Task<JsonDocument> GetJsonAsync(
        string pathAndQuery,
        CancellationToken cancellationToken,
        bool retryServerErrors = true)
    {
        using HttpResponseMessage response = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, NormalizePath(pathAndQuery)),
            cancellationToken,
            retryServerErrors);

        return await ReadJsonOrThrowAsync(response, cancellationToken);
    }

    public async Task<JsonDocument> PostJsonAsync(string pathAndQuery, object body, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(body);
        using HttpResponseMessage response = await SendAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, NormalizePath(pathAndQuery))
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                return request;
            },
            cancellationToken,
            retryServerErrors: true);

        return await ReadJsonOrThrowAsync(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken,
        bool retryServerErrors)
    {
        ResiliencePipeline<HttpResponseMessage> pipeline = retryServerErrors ? _pipeline : _throttlingPipeline;
        return await pipeline.ExecuteAsync(async token =>
        {
            HttpRequestMessage request = requestFactory();
            AccessToken tokenResult = await _credential.GetTokenAsync(new TokenRequestContext(ArmScopes), token);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.ParseAdd("ai-landscape-discovery/0.1");
            return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        }, cancellationToken);
    }

    private static async Task<JsonDocument> ReadJsonOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string body = response.Content is null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ArmRequestException(
                response.StatusCode,
                response.RequestMessage?.RequestUri?.ToString() ?? "<unknown>",
                body);
        }

        return JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
    }

    private ResiliencePipeline<HttpResponseMessage> BuildPipeline(bool retryServerErrors)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 8,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(response => IsTransient(response.StatusCode, retryServerErrors)),
                DelayGenerator = args =>
                {
                    TimeSpan? serverDelay = GetServerRetryDelay(args.Outcome.Result);
                    return new ValueTask<TimeSpan?>(serverDelay);
                },
                OnRetry = args =>
                {
                    if (_verbose)
                    {
                        Console.Error.WriteLine($"Retrying ARM request after {args.RetryDelay} due to HTTP {(int)args.Outcome.Result!.StatusCode}.");
                    }

                    args.Outcome.Result?.Dispose();
                    return default;
                }
            })
            .Build();
    }

    private static bool IsTransient(HttpStatusCode statusCode, bool retryServerErrors)
    {
        int code = (int)statusCode;
        return statusCode == HttpStatusCode.RequestTimeout
            || statusCode == (HttpStatusCode)429
            || (retryServerErrors && code >= 500);
    }

    private static TimeSpan? GetServerRetryDelay(HttpResponseMessage? response)
    {
        if (response is null)
        {
            return null;
        }

        if (response.Headers.RetryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (response.Headers.RetryAfter?.Date is { } date)
        {
            TimeSpan delay = date - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        if (TryReadSecondsHeader(response, "x-ms-retry-after-ms", out TimeSpan msDelay))
        {
            return msDelay;
        }

        if (TryReadSecondsHeader(response, "retry-after-ms", out TimeSpan retryAfterMs))
        {
            return retryAfterMs;
        }

        if (TryReadSecondsHeader(response, "x-ms-ratelimit-remaining-subscription-reads", out _))
        {
            return null;
        }

        return null;
    }

    private static bool TryReadSecondsHeader(HttpResponseMessage response, string headerName, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;
        if (!response.Headers.TryGetValues(headerName, out IEnumerable<string>? values))
        {
            return false;
        }

        string? value = values.FirstOrDefault();
        if (!double.TryParse(value, out double parsed))
        {
            return false;
        }

        delay = headerName.EndsWith("-ms", StringComparison.OrdinalIgnoreCase)
            ? TimeSpan.FromMilliseconds(parsed)
            : TimeSpan.FromSeconds(parsed);
        return true;
    }

    private static string NormalizePath(string pathAndQuery)
    {
        return pathAndQuery.StartsWith("/", StringComparison.Ordinal)
            ? pathAndQuery
            : "/" + pathAndQuery;
    }
}
