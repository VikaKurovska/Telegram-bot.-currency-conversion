using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SimpleTelegramBot
{
    class Bot
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly string _botToken;
        private readonly string _baseUrl;
        private readonly PrivatBankApi _privatBankApi;
        private readonly NbuExchangeRateApi _nbuExchangeRateApi;
        private readonly Dictionary<long, string[]> _conversionSteps;
        private readonly CurrencyConverter _currencyConverter;

        public Bot(string botToken)
        {
            _botToken = botToken;
            _baseUrl = $"https://api.telegram.org/bot{_botToken}";
            _privatBankApi = new PrivatBankApi();
            _nbuExchangeRateApi = new NbuExchangeRateApi();
            _conversionSteps = new Dictionary<long, string[]>();
            _currencyConverter = new CurrencyConverter(_privatBankApi, _nbuExchangeRateApi);
        }

        public async Task Start()
        {
            int offset = 0;

            while (true)
            {
                try
                {
                    string url = $"{_baseUrl}/getUpdates?offset={offset}";
                    string response = await _client.GetStringAsync(url);
                    dynamic updates = JsonConvert.DeserializeObject(response);

                    foreach (var update in updates.result)
                    {
                        offset = update.update_id + 1;
                        string messageText = update.message.text;
                        long chatId = update.message.chat.id;

                        if (messageText != null)
                        {
                            Console.WriteLine($"Користувач {chatId} написав: {messageText}");
                            await ProcessMessage(chatId, messageText);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Виникла помилка: {ex.Message}");
                }

                await Task.Delay(1000);
            }
        }

        private async Task ProcessMessage(long chatId, string text)
        {
            if (_conversionSteps.ContainsKey(chatId))
            {
                await HandleConversionSteps(chatId, text);
                return;
            }

            string[] commands = text.Split(' ');

            switch (commands[0].ToLower())
            {
                case "/start":
                    await ShowMainMenu(chatId);
                    break;
                case "/help":
                    await ShowHelp(chatId);
                    break;
                case "/current_privat":
                    await ShowPrivatExchangeRates(chatId);
                    break;
                case "/current_nbu":
                    await ShowNbuExchangeRates(chatId);
                    break;
                case "/convert":
                    _conversionSteps[chatId] = new string[3];
                    await SendMessage(chatId, "Введіть валюту, яку хочете конвертувати:");
                    break;
                default:
                    await SendMessage(chatId, "Я не розумію цю команду. Використовуйте /help для перегляду доступних опцій.");
                    break;
            }
        }

        private async Task HandleConversionSteps(long chatId, string text)
        {
            if (_conversionSteps[chatId][0] == null)
            {
                _conversionSteps[chatId][0] = text.ToUpper();
                await SendMessage(chatId, "Введіть суму для конвертації:");
            }
            else if (_conversionSteps[chatId][1] == null)
            {
                _conversionSteps[chatId][1] = text;
                await SendMessage(chatId, "Введіть валюту, в яку хочете конвертувати:");
            }
            else
            {
                _conversionSteps[chatId][2] = text.ToUpper();
                await ConvertCurrency(chatId, _conversionSteps[chatId][0], _conversionSteps[chatId][1], _conversionSteps[chatId][2]);
                _conversionSteps.Remove(chatId);
            }
        }

        private async Task ShowMainMenu(long chatId)
        {
            string message = "Виберіть опцію з меню:\n" +
                             "/current_privat - Переглянути поточні курси валют ПриватБанку\n" +
                             "/current_nbu - Переглянути поточні курси валют НБУ\n" +
                             "/convert - Конвертація валют\n" +
                             "/help - Інформація про бота";
            await SendMessage(chatId, message);
        }

        private async Task ShowHelp(long chatId)
        {
            string message = "Цей бот допоможе вам з переглядом поточних курсів валют та конвертацією валют за актуальними курсами.\n" +
                             "Доступні команди:\n" +
                             "/current_privat - Переглянути поточні курси валют ПриватБанку\n" +
                             "/current_nbu - Переглянути поточні курси валют НБУ\n" +
                             "/convert - Конвертація валют\n" +
                             "/help - Інформація про бота";
            await SendMessage(chatId, message);
        }

        private async Task ShowPrivatExchangeRates(long chatId)
        {
            try
            {
                var privatRates = await _privatBankApi.GetExchangeRates();
                string message = "Поточний курс валют (ПриватБанк):\n";
                message += string.Join("\n", privatRates.Select(rate => $"{rate.Key}: {rate.Value}"));
                await SendMessage(chatId, message);
            }
            catch (Exception ex)
            {
                await SendMessage(chatId, $"Не вдалося отримати поточні курси валют ПриватБанку. Помилка: {ex.Message}");
            }
        }

        private async Task ShowNbuExchangeRates(long chatId)
        {
            try
            {
                var nbuRates = await _nbuExchangeRateApi.GetExchangeRates();
                string message = "Поточний курс валют (НБУ):\n";
                message += string.Join("\n", nbuRates.Select(rate => $"{rate.Key}: {rate.Value}"));
                await SendMessage(chatId, message);
            }
            catch (Exception ex)
            {
                await SendMessage(chatId, $"Не вдалося отримати поточні курси валют НБУ. Помилка: {ex.Message}");
            }
        }

        private async Task ConvertCurrency(long chatId, string fromCurrency, string amount, string toCurrency)
        {
            if (decimal.TryParse(amount, out decimal amountDecimal))
            {
                try
                {
                    var (convertedAmount, bankInfo) = await _currencyConverter.ConvertCurrency(fromCurrency, toCurrency, amountDecimal);
                    await SendMessage(chatId, $"Конвертація {amountDecimal} {fromCurrency} в {toCurrency}:\nРезультат: {convertedAmount}\nДжерела: {bankInfo}");
                }
                catch (Exception ex)
                {
                    await SendMessage(chatId, $"Не вдалося виконати конвертацію. Помилка: {ex.Message}");
                }
            }
            else
            {
                await SendMessage(chatId, "Невірний формат суми.");
            }
        }



        private async Task SendMessage(long chatId, string text)
        {
            string url = $"{_baseUrl}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString(text)}";
            await _client.GetStringAsync(url);
        }
    }
}
