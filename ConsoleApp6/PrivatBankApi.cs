using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class PrivatBankApi
{
    private readonly HttpClient _client = new HttpClient();
    private readonly string _baseUrl = "https://api.privatbank.ua/p24api/exchange_rates?json&date=";

    public async Task<Dictionary<string, decimal>> GetExchangeRates()
    {
        string date = DateTime.Now.ToString("dd.MM.yyyy");
        string url = _baseUrl + date;
        HttpResponseMessage response = await _client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Помилка при отриманні даних з ПриватБанку: {response.StatusCode}");
        }

        var result = JsonConvert.DeserializeObject<PrivatBankApiResult>(await response.Content.ReadAsStringAsync());
        var exchangeRates = result.ExchangeRate.ToDictionary(r => r.Currency, r => r.SaleRateNB);

        return exchangeRates;
    }
}

public class PrivatBankApiResult
{
    [JsonProperty("exchangeRate")]
    public List<ExchangeRate> ExchangeRate { get; set; }
}

public class ExchangeRate
{
    [JsonProperty("currency")]
    public string Currency { get; set; }

    [JsonProperty("saleRateNB")]
    public decimal SaleRateNB { get; set; }
}
