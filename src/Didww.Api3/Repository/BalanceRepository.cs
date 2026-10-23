using Didww.Api3.Exception;
using Didww.Api3.Http;
using Didww.Api3.Resource;
using JsonApiSerializer;
using Newtonsoft.Json;

namespace Didww.Api3.Repository;

public class BalanceRepository : ReadOnlyRepository<Balance>
{
    public BalanceRepository(HttpClient httpClient, JsonSerializerSettings serializerSettings,
        string baseUrl, string endpoint)
        : base(httpClient, serializerSettings, baseUrl, endpoint)
    {
    }

    public async Task<ApiResponse<Balance>> FindAsync(QueryParams? queryParams = null)
    {
        var url = BuildUrl(queryParams: queryParams);
        var response = await HttpClient.GetAsync(url);
        await HandleErrorResponseAsync(response);
        var body = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<Balance>(body, SerializerSettings)!;
        return new ApiResponse<Balance>(data, ExtractMeta(body));
    }
}
