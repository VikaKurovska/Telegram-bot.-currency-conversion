using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CurrencyConverter
{
    private readonly PrivatBankApi _privatBankApi;
    private readonly NbuExchangeRateApi _nbuExchangeRateApi;

    public CurrencyConverter(PrivatBankApi privatBankApi, NbuExchangeRateApi nbuExchangeRateApi)
    {
        _privatBankApi = privatBankApi;
        _nbuExchangeRateApi = nbuExchangeRateApi;
    }

    public async Task<(decimal, string)> ConvertCurrency(string fromCurrency, string toCurrency, decimal amount)
    {
        var privatRates = await _privatBankApi.GetExchangeRates();
        var nbuRates = await _nbuExchangeRateApi.GetExchangeRates();

        // Створюємо словник для обрання найкращого курсу
        var allRates = new Dictionary<string, (decimal, string)>();

        // Додаємо курси з ПриватБанку
        foreach (var rate in privatRates)
        {
            allRates[rate.Key] = (rate.Value, "PrivatBank");
        }

        // Додаємо курси з НБУ, змінюючи значення якщо вони кращі
        foreach (var rate in nbuRates)
        {
            if (!allRates.ContainsKey(rate.Key) || rate.Value < allRates[rate.Key].Item1)
            {
                allRates[rate.Key] = (rate.Value, "NBU");
            }
        }

        // Отримуємо курс для валют, між якими конвертуємо
        if (!allRates.TryGetValue(fromCurrency.ToUpper(), out var fromRateTuple))
        {
            throw new Exception($"Курс для валюти {fromCurrency} не знайдено.");
        }

        if (!allRates.TryGetValue(toCurrency.ToUpper(), out var toRateTuple))
        {
            throw new Exception($"Курс для валюти {toCurrency} не знайдено.");
        }

        decimal fromRate = fromRateTuple.Item1;
        decimal toRate = toRateTuple.Item1;

        // Правильна формула конвертації
        decimal result = amount * (fromRate / toRate);
        return (Math.Round(result, 2), $"From {fromRateTuple.Item2}, To {toRateTuple.Item2}");
    }

}