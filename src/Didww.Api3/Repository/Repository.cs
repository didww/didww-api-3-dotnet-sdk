using System.Reflection;
using System.Text;
using Didww.Api3.Converter;
using Didww.Api3.Exception;
using Didww.Api3.Http;
using Didww.Api3.Resource;
using JsonApiSerializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Didww.Api3.Repository;

public class Repository<T> : ReadOnlyRepository<T> where T : BaseResource
{
    public Repository(HttpClient httpClient, JsonSerializerSettings serializerSettings,
        string baseUrl, string endpoint)
        : base(httpClient, serializerSettings, baseUrl, endpoint)
    {
    }

    public async Task<ApiResponse<T>> CreateAsync(T resource, QueryParams? queryParams = null,
        CancellationToken cancellationToken = default)
    {
        var url = BaseUrl + "/" + Endpoint + (queryParams?.ToQueryString() ?? "");
        var payload = JsonConvert.SerializeObject(resource, SerializerSettings);
        var content = new StringContent(payload, Encoding.UTF8, JsonApiMediaType);
        var body = await SendAsync(HttpMethod.Post, url, content, cancellationToken).ConfigureAwait(false);
        var data = JsonConvert.DeserializeObject<T>(body, SerializerSettings)!;
        EnableDirtyTracking(data);
        return new ApiResponse<T>(data, ExtractMeta(body));
    }

    public async Task<ApiResponse<T>> UpdateAsync(T resource, QueryParams? queryParams = null,
        CancellationToken cancellationToken = default)
    {
        var id = resource.Id ?? throw new DidwwClientException("Resource ID is null");
        var url = BaseUrl + "/" + Endpoint + "/" + id + (queryParams?.ToQueryString() ?? "");

        string payload;
        try
        {
            DirtySerializationContext.EnableDirtyOnlyMode();
            payload = JsonConvert.SerializeObject(resource, SerializerSettings);
            payload = EnsureDirtyNullRelationships(resource, payload);
        }
        finally
        {
            DirtySerializationContext.DisableDirtyOnlyMode();
        }

        var content = new StringContent(payload, Encoding.UTF8, JsonApiMediaType);
        var body = await SendAsync(HttpMethod.Patch, url, content, cancellationToken).ConfigureAwait(false);
        var data = JsonConvert.DeserializeObject<T>(body, SerializerSettings)!;
        EnableDirtyTracking(data);
        return new ApiResponse<T>(data, ExtractMeta(body));
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var url = BaseUrl + "/" + Endpoint + "/" + id;
        await SendAsync(HttpMethod.Delete, url, null, cancellationToken).ConfigureAwait(false);
    }

    private string EnsureDirtyNullRelationships(T resource, string payload)
    {
        try
        {
            // Parsed as the serializer reads: numbers stay decimal with their
            // scale and date-like strings stay strings.
            using var reader = new JsonTextReader(new StringReader(payload))
            {
                FloatParseHandling = SerializerSettings.FloatParseHandling,
                DateParseHandling = SerializerSettings.DateParseHandling,
                DateTimeZoneHandling = SerializerSettings.DateTimeZoneHandling,
                Culture = SerializerSettings.Culture
            };
            var rootNode = JObject.Load(reader);
            if (rootNode["data"] is not JObject dataNode)
                return payload;

            var nulledRelationships = GetDeclaredProperties(resource)
                .Where(IsRelationshipProperty)
                .Where(prop => resource.IsFieldDirty(GetJsonPropertyName(prop)) && prop.GetValue(resource) == null)
                .ToDictionary(GetJsonPropertyName, prop => new JObject
                {
                    ["data"] = IsListRelationship(prop) ? (JToken)new JArray() : JValue.CreateNull()
                });

            if (nulledRelationships.Count == 0)
                return payload;

            var relationshipsNode = dataNode["relationships"] as JObject ?? new JObject();
            foreach (var (relName, relNode) in nulledRelationships)
                relationshipsNode[relName] = relNode;

            dataNode["relationships"] = relationshipsNode;
            return rootNode.ToString(Formatting.None);
        }
        catch (System.Exception)
        {
            return payload;
        }
    }

    private static bool IsRelationshipProperty(PropertyInfo prop) =>
        typeof(BaseResource).IsAssignableFrom(prop.PropertyType) || IsListRelationship(prop);

    private static bool IsListRelationship(PropertyInfo prop) =>
        prop.PropertyType.IsGenericType &&
        prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
        typeof(BaseResource).IsAssignableFrom(prop.PropertyType.GetGenericArguments()[0]);

    private static string GetJsonPropertyName(PropertyInfo prop) =>
        prop.GetCustomAttributes(typeof(JsonPropertyAttribute), false)
            .OfType<JsonPropertyAttribute>()
            .FirstOrDefault()?.PropertyName ?? prop.Name;
}
