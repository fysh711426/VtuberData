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

        protected static async Task<T> Retry<T>(Func<Task<T>> func)
        {
            var count = 0;
            while(true)
            {
                try
                {
                    return await func();
                }
                catch
                {
                    count++;
                    Console.WriteLine($"Retry...{count}");
                    await Task.Delay(20000);
                }
            }
        }

        protected static readonly Random _random = new Random();
        protected static int DelayRandom(int min, int max)
        {
            return _random.Next(min, max);
        }
        protected static int DelayRandom()
        {
            return DelayRandom(1000, 1500);
        }
        protected static async Task SleepRandom(int min, int max)
        {
            await Task.Delay(DelayRandom(min, max));
        }
        protected static async Task SleepRandom()
        {
            await SleepRandom(1000, 1500);
        }
    }
}
