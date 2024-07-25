using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class NbuExchangeRateApi
{
    private readonly HttpClient _client = new HttpClient();
    private readonly string _baseUrl = "https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?json";

    public async Task<Dictionary<string, decimal>> GetExchangeRates()
    {
        HttpResponseMessage response = await _client.GetAsync(_baseUrl);
        response.EnsureSuccessStatusCode();

        var result = JsonConvert.DeserializeObject<List<NbuApiResult>>(await response.Content.ReadAsStringAsync());
        var exchangeRates = new Dictionary<string, decimal>();

        foreach (var rate in result)
        {
            exchangeRates[rate.cc] = rate.rate;
        }

        return exchangeRates;
    }
}

public class NbuApiResult
{
    public string cc { get; set; }
    public decimal rate { get; set; }
}
