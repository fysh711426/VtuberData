using VtuberData.Crawlers;

namespace VtuberData
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var workDir = AppDomain.CurrentDomain.BaseDirectory;
            var dataDir = Path.Combine(workDir, "Data");
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);
            var filePath = Path.Combine(dataDir, "Vtubers.csv");
            var vtuberCrawler = new VtuberCrawler();
            await vtuberCrawler.Load(filePath);
            await vtuberCrawler.CreateOrUpdateVtubersTw();
            await vtuberCrawler.CreateOrUpdateVtubersJp();
            await vtuberCrawler.Save();
        }
    }
}
