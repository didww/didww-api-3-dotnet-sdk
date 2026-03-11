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
    private const string JsonApiMediaType = "application/vnd.api+json";

    public Repository(HttpClient httpClient, JsonSerializerSettings serializerSettings,
        string baseUrl, string endpoint)
        : base(httpClient, serializerSettings, baseUrl, endpoint)
    {
    }

    public async Task<ApiResponse<T>> CreateAsync(T resource, QueryParams? queryParams = null)
    {
        var url = BaseUrl + "/" + Endpoint + (queryParams?.ToQueryString() ?? "");
        var payload = JsonConvert.SerializeObject(resource, SerializerSettings);
        var content = new StringContent(payload, Encoding.UTF8, JsonApiMediaType);
        using var response = await HttpClient.PostAsync(url, content);
        await HandleErrorResponseAsync(response);
        var body = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<T>(body, SerializerSettings)!;
        EnableDirtyTracking(data);
        return new ApiResponse<T>(data, ExtractMeta(body));
    }

    public async Task<ApiResponse<T>> UpdateAsync(T resource, QueryParams? queryParams = null)
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
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
        using var response = await HttpClient.SendAsync(request);
        await HandleErrorResponseAsync(response);
        var body = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<T>(body, SerializerSettings)!;
        EnableDirtyTracking(data);
        return new ApiResponse<T>(data, ExtractMeta(body));
    }

    public async Task DeleteAsync(string id)
    {
        var url = BaseUrl + "/" + Endpoint + "/" + id;
        using var response = await HttpClient.DeleteAsync(url);
        await HandleErrorResponseAsync(response);
    }

    private string EnsureDirtyNullRelationships(T resource, string payload)
    {
        try
        {
            var rootNode = JObject.Parse(payload);
            var dataNode = rootNode["data"] as JObject;
            if (dataNode == null)
                return payload;

            var relationshipsNode = dataNode["relationships"] as JObject ?? new JObject();
            bool changed = false;

            for (var type = resource.GetType();
                 type != null && typeof(BaseResource).IsAssignableFrom(type);
                 type = type.BaseType)
            {
                foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    if (!typeof(BaseResource).IsAssignableFrom(prop.PropertyType) &&
                        !(prop.PropertyType.IsGenericType &&
                          prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                          typeof(BaseResource).IsAssignableFrom(prop.PropertyType.GetGenericArguments()[0])))
                        continue;

                    var jsonProp = prop.GetCustomAttributes(typeof(JsonPropertyAttribute), false)
                        .OfType<JsonPropertyAttribute>().FirstOrDefault();
                    if (jsonProp == null)
                        continue;

                    var relName = jsonProp.PropertyName ?? prop.Name;
                    if (!resource.IsFieldDirty(relName))
                        continue;

                    var value = prop.GetValue(resource);
                    if (value == null)
                    {
                        var relNode = new JObject();
                        if (prop.PropertyType.IsGenericType &&
                            prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            relNode["data"] = new JArray();
                        }
                        else
                        {
                            relNode["data"] = JValue.CreateNull();
                        }
                        relationshipsNode[relName] = relNode;
                        changed = true;
                    }
                }
            }

            if (!changed)
                return payload;

            dataNode["relationships"] = relationshipsNode;
            return rootNode.ToString(Formatting.None);
        }
        catch (System.Exception)
        {
            return payload;
        }
    }
}
