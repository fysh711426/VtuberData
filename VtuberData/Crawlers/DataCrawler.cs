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
using VtuberData.Storages;
using YoutubeParser;
using YoutubeParser.Channels;
using YoutubeParser.ChannelVideos;
using YoutubeParser.Commons;
using YoutubeParser.Utils;

namespace VtuberData.Crawlers
{
    public class DataCrawler : BaseCrawler
    {
        private DateTime _now;
        private DbContext _db;
        public DataCrawler(DateTime now, DbContext db)
        {
            _now = now;
            _db = db;
        }

        public async Task Save()
        {
            await _db.Datas.Save(list => list.OrderBy(it => it.Id));
        }

        public async Task CreateAndCalcData()
        {
            var index = 0;
            var vtubers = _db.Vtubers?.GetAll() ?? new List<Vtuber>();
            var count = vtubers.Count;
            foreach (var vtuber in vtubers)
            {
                index++;
                if (vtuber.Status != Status.Activity)
                    continue;

                var youtube = new YoutubeClient();
                var data = _db.Datas.Get(vtuber.ChannelUrl);
                if (data == null)
                {
                    var channel = await youtube.Channel.GetAsync(vtuber.ChannelUrl);
                    data = new Data
                    {
                        ChannelUrl = vtuber.ChannelUrl,
                        SubscriberCount = channel.SubscriberCount,
                        ViewCount = channel.ViewCount
                    };
                }

                var videosByDay7 = new List<ChannelVideo>();
                var videosByDay30 = new List<ChannelVideo>();

                var videos = youtube.Channel.GetVideosAsync(vtuber.ChannelUrl);
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

                data = new Data
                {
                    Id = vtuber.Id,
                    ChannelUrl = vtuber.ChannelUrl,
                    Name = vtuber.Name,
                    SubscriberCount = data.SubscriberCount,
                    ViewCount = data.ViewCount,
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
                _db.Datas.Create(data);

                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"[{time}][{index}/{count}] Create data {vtuber.Name}");
                await SleepRandom();
            }
        }
    }
}
