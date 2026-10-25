using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ReleaseDeck.Portal.Services;

/// <summary>Calls same-origin authenticated actions once; uncertain writes are never automatically repeated.</summary>
public sealed class ReleaseApi(HttpClient http, ILogger<ReleaseApi> logger)
{
    const bool IsLogEnabled = true;
    const string Prefix = "/api/commerce/release-deck/";
    public Task<T> Get<T>(string route) => Send<T>(HttpMethod.Get, Prefix + route, null);
    public Task<T> Post<T>(string route, object body) => Send<T>(HttpMethod.Post, Prefix + route, body);
    public Task<AccountStatus> Account() => Send<AccountStatus>(HttpMethod.Get, "/api/commerce/status", null);
    /// <summary>Preserves service error codes, excludes private bodies and URL identifiers from diagnostics.</summary>
    async Task<T> Send<T>(HttpMethod method, string route, object? body)
    {
        using var message = new HttpRequestMessage(method, route);
        if (body is not null) message.Content = JsonContent.Create(body);
        if (IsLogEnabled) logger.LogInformation("Release request started; method={Method}; bodyPresent={BodyPresent}", method.Method, body is not null);
        using var response = await http.SendAsync(message);
        if (IsLogEnabled) logger.LogInformation("Release response received; status={Status}", (int)response.StatusCode);
        if (!response.IsSuccessStatusCode)
        {
            ServiceFailure? failure = null;
            try { failure = await response.Content.ReadFromJsonAsync<ServiceFailure>(); } catch (JsonException) { }
            var text = response.StatusCode switch { HttpStatusCode.Conflict => "This item changed. Your local edits are safe. Refresh its status before trying again.", HttpStatusCode.Unauthorized => "Sign in to use online releases.", HttpStatusCode.RequestEntityTooLarge => "This release is too large to save online. Shorten its descriptions or keep a project file.", HttpStatusCode.ServiceUnavailable => "This connection is not available yet. Your local release is safe.", _ => failure?.Error ?? "The action did not finish. Keep your local release and refresh its status." };
            throw new ReleaseFailure(response.StatusCode, failure?.Code ?? "request", text);
        }
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new InvalidDataException("The response was empty. Refresh the release status.");
    }
}
/// <summary>An actionable refusal with a status and code; request credentials are not part of the exception.</summary>
public sealed class ReleaseFailure(HttpStatusCode status, string code, string message) : Exception(message) { public HttpStatusCode Status { get; } = status; public string Code { get; } = code; }
