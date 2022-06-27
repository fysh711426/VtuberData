using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeParser.Utils;

namespace VtuberData.Crawlers
{
    public class BaseCrawler
    {
        protected static readonly string userAgent =
            @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.99 Safari/537.36";
        protected readonly HttpClient _httpClient;

        public BaseCrawler()
        {
            _httpClient = Http.Client;
            _httpClient.DefaultRequestHeaders.ConnectionClose = true;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        }

        protected static readonly Random _random = new Random();
        protected static async Task SleepRandom(int min, int max)
        {
            var num = _random.Next(min, max);
            await Task.Delay(num);
        }
        protected static async Task SleepRandom()
        {
            await SleepRandom(200, 700);
        }
    }
}
