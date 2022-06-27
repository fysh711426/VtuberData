using VtuberData.Crawlers;

namespace VtuberData
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var action = "";
            var waitfor = false;
            if (args.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("VtuberData");
                Console.ResetColor();
                Console.Write(" > ");
                action = Console.ReadLine();
                waitfor = true;
            }
            else
            {
                action = args[0];
            }

            try
            {
                var workDir = AppDomain.CurrentDomain.BaseDirectory;
                var dataDir = Path.Combine(workDir, "Data");
                if (!Directory.Exists(dataDir))
                    Directory.CreateDirectory(dataDir);
                var vtuberFilePath = Path.Combine(dataDir, "Vtubers.csv");

                if (action == "vtuber")
                {
                    var vtuberCrawler = new VtuberCrawler();
                    await vtuberCrawler.Load(vtuberFilePath);
                    await vtuberCrawler.CreateOrUpdateVtubersTw();
                    await vtuberCrawler.CreateOrUpdateVtubersJp();
                    await vtuberCrawler.Save(vtuberFilePath);
                }
                else if (action == "data")
                {
                    var now = DateTime.Now;
                    var month = now.ToString("yyyy-MM");
                    var time = now.ToString("yyyy-MM-dd_HH-mm-ss");
                    var monthDir = Path.Combine(dataDir, month);
                    if (!Directory.Exists(monthDir))
                        Directory.CreateDirectory(monthDir);
                    var dataFilePath = Path.Combine(monthDir, $"Data_{time}.csv");
                    var dataCrawler = new DataCrawler();
                    await dataCrawler.Load(vtuberFilePath);
                    await dataCrawler.CreateAndCalcData();
                    await dataCrawler.Save(dataFilePath);
                }
                else
                {
                    throw new Exception("Wrong action.");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" > {ex.Message}");
                Console.ResetColor();
            }

            if (waitfor)
                Console.ReadLine();
        }
    }
}
