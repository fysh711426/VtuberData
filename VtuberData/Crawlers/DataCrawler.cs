using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VtuberData.Extensions;
using VtuberData.Models;
using YoutubeParser;
using YoutubeParser.Channels;
using YoutubeParser.Models;
using YoutubeParser.Utils;

namespace VtuberData.Crawlers
{
    public class DataCrawler : BaseCrawler
    {
        private string _now = "";
        private List<Data> _dataList = new List<Data>();
        private List<Vtuber> _vtuberList = new List<Vtuber>();
        private CsvConfiguration _configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            ShouldQuote = (args) => true
        };

        public async Task Load(string path)
        {
            _now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _vtuberList = new List<Vtuber>();

            if (File.Exists(path))
            {
                using (var reader = new StreamReader(path, new UTF8Encoding(true)))
                using (var csv = new CsvReader(reader, _configuration))
                {
                    var records = await csv.GetAllRecordsAsync<Vtuber>();
                    _vtuberList = records.ToList();
                }
            }
        }

        public async Task Save(string path)
        {
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            using (var csv = new CsvWriter(writer, _configuration))
            {
                csv.WriteHeader<Data>();
                csv.NextRecord();

                if (_dataList.Count > 0)
                {
                    await csv.WriteRecordsAsync(_dataList);
                }
            }
            var _time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var ts = DateTime.Parse(_time) - DateTime.Parse(_now);
            var str = (ts.Hours.ToString("00") == "00" ? "" : ts.Hours.ToString("00") + "h") + ts.Minutes.ToString("00") + "m" + ts.Seconds.ToString("00") + "s";
            Console.WriteLine($"[{_time}] Save data success. @ {str}");
        }

        public async Task CreateAndCalcData()
        {
            _dataList = new List<Data>();

            var index = 0;
            var count = _vtuberList.Count;
            foreach (var vtuber in _vtuberList)
            {
                index++;
                if (vtuber.Status != Status.Activity)
                    continue;
                var channelId = vtuber.ChannelUrl.Replace("https://www.youtube.com/channel/", "");
                var youtube = new YoutubeClient();
                var channel = await youtube.Channel.GetAsync(channelId);
                var videos = youtube.Channel.GetVideosAsync(channelId);
                var videosByDay7 = new List<ChannelVideo>();
                var videosByDay30 = new List<ChannelVideo>();
                await foreach (var item in videos)
                {
                    if (item.IsShorts)
                        continue;
                    if (item.VideoStatus != VideoStatus.Default)
                        continue;
                    var seconds = item.PublishedTimeSeconds;
                    if (seconds >= TimeSeconds.Month)
                        break;
                    if (seconds < TimeSeconds.Week)
                        videosByDay7.Add(item);
                    videosByDay30.Add(item);
                }

                var medianViewCountDay7 = videosByDay7
                    .Median(it => it.ViewCount);
                var medianViewCountDay30 = videosByDay30
                    .Median(it => it.ViewCount);
                var highestDay7 = videosByDay7
                    .OrderByDescending(it => it.ViewCount)
                    .FirstOrDefault();
                var highestDay30 = videosByDay30
                    .OrderByDescending(it => it.ViewCount)
                    .FirstOrDefault();

                var data = new Data
                {
                    Id = vtuber.Id,
                    ChannelUrl = vtuber.ChannelUrl,
                    Name = vtuber.Name,
                    SubscriberCount = channel.SubscriberCount,
                    ViewCount = channel.ViewCount,
                    MedianViewCountDay7 = medianViewCountDay7,
                    HighestViewCountDay7 = highestDay7?.ViewCount ?? 0,
                    HighestViewVideoUrlDay7 = highestDay7?.ShortUrl ?? "",
                    HighestViewVideoTitleDay7 = highestDay7?.Title ?? "",
                    HighestViewVideoThumbnailDay7 = highestDay7?.Thumbnails?.LastOrDefault()?.Url ?? "",
                    HighestViewVideoRichThumbnailDay7 = highestDay7?.RichThumbnail?.Url ?? "",
                    MedianViewCountDay30 = medianViewCountDay30,
                    HighestViewCountDay30 = highestDay30?.ViewCount ?? 0,
                    HighestViewVideoUrlDay30 = highestDay30?.ShortUrl ?? "",
                    HighestViewVideoTitleDay30 = highestDay30?.Title ?? "",
                    HighestViewVideoThumbnailDay30 = highestDay30?.Thumbnails?.LastOrDefault()?.Url ?? "",
                    HighestViewVideoRichThumbnailDay30 = highestDay30?.RichThumbnail?.Url ?? ""
                };
                _dataList.Add(data);

                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"[{time}][{index}/{count}] Create data {vtuber.Name}");
                await SleepRandom();
            }
        }
    }
}
