using System.Net;

namespace AiLandscapeDiscovery.Cli.Azure;

public sealed class ArmRequestException : Exception
{
    public ArmRequestException(HttpStatusCode statusCode, string requestUri, string responseBody)
        : base($"ARM request failed with {(int)statusCode} {statusCode}: {requestUri}")
    {
        StatusCode = statusCode;
        RequestUri = requestUri;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }

    public string RequestUri { get; }

    public string ResponseBody { get; }
}
