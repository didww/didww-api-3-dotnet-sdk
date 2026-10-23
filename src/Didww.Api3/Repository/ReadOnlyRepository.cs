using System.Collections;
using System.Net.Http.Headers;
using System.Reflection;
using Didww.Api3.Converter;
using Didww.Api3.Exception;
using Didww.Api3.Http;
using Didww.Api3.Resource;
using JsonApiSerializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Didww.Api3.Repository;

public class ReadOnlyRepository<T> where T : BaseResource
{
    private protected const string JsonApiMediaType = "application/vnd.api+json";

    // Resources called without an access token.
    private static readonly HashSet<string> PublicEndpoints = new(StringComparer.Ordinal) { "public_keys" };

    protected readonly HttpClient HttpClient;
    protected readonly JsonSerializerSettings SerializerSettings;
    protected readonly string BaseUrl;
    protected readonly string Endpoint;

    public ReadOnlyRepository(HttpClient httpClient, JsonSerializerSettings serializerSettings,
        string baseUrl, string endpoint)
    {
        HttpClient = httpClient;
        SerializerSettings = serializerSettings;
        BaseUrl = baseUrl;
        Endpoint = endpoint;
    }

    protected string BuildUrl(string? id = null, QueryParams? queryParams = null)
    {
        var url = BaseUrl + "/" + Endpoint;
        if (id != null)
            url += "/" + id;
        url += queryParams?.ToQueryString() ?? "";
        return url;
    }

    public async Task<ApiResponse<List<T>>> ListAsync(QueryParams? queryParams = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(queryParams: queryParams);
        var body = await SendAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);
        var data = JsonConvert.DeserializeObject<List<T>>(body, SerializerSettings) ?? new List<T>();
        var meta = ExtractMeta(body);
        EnableDirtyTracking(data);
        return new ApiResponse<List<T>>(data, meta);
    }

    public async Task<ApiResponse<T>> FindAsync(string id, QueryParams? queryParams = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(id, queryParams);
        var body = await SendAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);
        var data = JsonConvert.DeserializeObject<T>(body, SerializerSettings)!;
        var meta = ExtractMeta(body);
        EnableDirtyTracking(data);
        return new ApiResponse<T>(data, meta);
    }

    /// <summary>
    /// Sends a JSON:API request and returns the response body, raising
    /// <see cref="DidwwApiException"/> on a non-success status. The response is disposed.
    /// </summary>
    protected async Task<string> SendAsync(HttpMethod method, string url, HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonApiMediaType));
        if (PublicEndpoints.Contains(Endpoint))
            request.Options.Set(DidwwRequestOptions.Audience, DidwwRequestOptions.NoAudience);

        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw BuildApiException((int)response.StatusCode, body);
        return body;
    }

    protected async Task HandleErrorResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw BuildApiException((int)response.StatusCode, body);
    }

    private static DidwwApiException BuildApiException(int status, string body)
    {
        var errors = new List<ApiError>();
        try
        {
            var root = JObject.Parse(body);
            var errorsNode = root["errors"] as JArray;
            if (errorsNode != null)
            {
                foreach (var errorNode in errorsNode)
                {
                    errors.Add(errorNode.ToObject<ApiError>()!);
                }
            }
        }
        catch
        {
            // ignore parse errors
        }

        if (errors.Count == 0)
            return new DidwwApiException(status, body);

        return new DidwwApiException(status, errors);
    }

    protected Dictionary<string, object>? ExtractMeta(string body)
    {
        try
        {
            var root = JObject.Parse(body);
            var meta = root["meta"];
            if (meta != null)
                return meta.ToObject<Dictionary<string, object>>();
        }
        catch
        {
            // ignore
        }
        return null;
    }

    protected void EnableDirtyTracking(T resource)
    {
        if (resource is BaseResource br)
            EnableDirtyTrackingRecursive(br, new HashSet<BaseResource>(ReferenceEqualityComparer.Instance));
    }

    protected void EnableDirtyTracking(List<T> resources)
    {
        var visited = new HashSet<BaseResource>(ReferenceEqualityComparer.Instance);
        foreach (var resource in resources)
        {
            if (resource is BaseResource br)
                EnableDirtyTrackingRecursive(br, visited);
        }
    }

    private static void EnableDirtyTrackingRecursive(BaseResource resource, HashSet<BaseResource> visited)
    {
        if (!visited.Add(resource))
            return;

        resource.EnableDirtyTracking();

        ForEachDeclaredProperty(resource, prop =>
        {
            var value = prop.GetValue(resource);
            if (value is BaseResource relatedResource)
            {
                EnableDirtyTrackingRecursive(relatedResource, visited);
            }
            else if (value is IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is BaseResource relatedItem)
                        EnableDirtyTrackingRecursive(relatedItem, visited);
                }
            }
        });
    }

    protected static IEnumerable<PropertyInfo> GetDeclaredProperties(BaseResource resource)
    {
        for (var type = resource.GetType();
             type != null && typeof(BaseResource).IsAssignableFrom(type);
             type = type.BaseType)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                yield return prop;
        }
    }

    protected static void ForEachDeclaredProperty(BaseResource resource, Action<PropertyInfo> action)
    {
        foreach (var prop in GetDeclaredProperties(resource))
            action(prop);
    }
}
