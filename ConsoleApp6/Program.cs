using System;
using System.Threading.Tasks;

namespace SimpleTelegramBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string botToken = "7106063173:AAEN4WtM3MUxvtaWbto9FxLd9-IPDek4Qh8";
            Bot bot = new Bot(botToken);
            await bot.Start();
        }
    }
}
