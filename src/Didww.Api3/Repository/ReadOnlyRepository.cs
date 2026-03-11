using System.Collections;
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

    public async Task<ApiResponse<List<T>>> ListAsync(QueryParams? queryParams = null)
    {
        var url = BaseUrl + "/" + Endpoint + (queryParams?.ToQueryString() ?? "");
        using var response = await HttpClient.GetAsync(url);
        await HandleErrorResponseAsync(response);
        var body = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<List<T>>(body, SerializerSettings) ?? new List<T>();
        var meta = ExtractMeta(body);
        EnableDirtyTracking(data);
        return new ApiResponse<List<T>>(data, meta);
    }

    public async Task<ApiResponse<T>> FindAsync(string id, QueryParams? queryParams = null)
    {
        var url = BaseUrl + "/" + Endpoint + "/" + id + (queryParams?.ToQueryString() ?? "");
        using var response = await HttpClient.GetAsync(url);
        await HandleErrorResponseAsync(response);
        var body = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<T>(body, SerializerSettings)!;
        var meta = ExtractMeta(body);
        EnableDirtyTracking(data);
        return new ApiResponse<T>(data, meta);
    }

    protected async Task HandleErrorResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        throw DidwwApiException.FromResponseBody((int)response.StatusCode, body);
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

        for (var type = resource.GetType();
             type != null && typeof(BaseResource).IsAssignableFrom(type);
             type = type.BaseType)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
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
            }
        }
    }
}
