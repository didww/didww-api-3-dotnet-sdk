using Didww.Api3.Exception;
using Didww.Api3.Http;
using Didww.Api3.Resource;
using JsonApiSerializer;
using Newtonsoft.Json;

namespace Didww.Api3.Repository;

public class SingletonRepository<T> : ReadOnlyRepository<T> where T : BaseResource
{
    public SingletonRepository(HttpClient httpClient, JsonSerializerSettings serializerSettings,
        string baseUrl, string endpoint)
        : base(httpClient, serializerSettings, baseUrl, endpoint)
    {
    }

    public async Task<ApiResponse<T>> FindAsync(QueryParams? queryParams = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(queryParams: queryParams);
        var body = await SendAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);
        var data = JsonConvert.DeserializeObject<T>(body, SerializerSettings)!;
        return new ApiResponse<T>(data, ExtractMeta(body));
    }
}
