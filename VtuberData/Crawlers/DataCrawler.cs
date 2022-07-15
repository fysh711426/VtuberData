using VtuberData.Extensions;
using VtuberData.Models;
using VtuberData.Storages;
using YoutubeParser;
using YoutubeParser.ChannelVideos;
using YoutubeParser.Extensions;
using YoutubeParser.Shares;

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
            var vtubers = _db.Vtubers.GetAll();
            var count = vtubers.Count;
            foreach (var vtuber in vtubers)
            {
                index++;
                if (vtuber.Status != Status.Activity)
                    continue;

                var youtube = new YoutubeClient(() => DelayRandom());
                var data = _db.Datas.Get(vtuber.ChannelUrl);
                if (data == null)
                {
                    var channel = await Retry(() => 
                        youtube.Channel.GetAsync(vtuber.ChannelUrl));
                    data = new Data
                    {
                        ChannelUrl = vtuber.ChannelUrl,
                        SubscriberCount = channel.SubscriberCount,
                        ViewCount = channel.ViewCount
                    };
                }

                var videosByDay7 = new List<ChannelVideo>();
                var videosByDay30 = new List<ChannelVideo>();

                var videos = await Retry(() =>
                    youtube.Channel.GetVideosAsync(vtuber.ChannelUrl)
                        .BreakOn(it => it.PublishedTimeSeconds >= TimeSeconds.Month)
                        .ToListAsync().AsTask());

                var first = null as ChannelVideo;
                var second = null as ChannelVideo;
                foreach (var item in videos)
                {
                    if (first == null)
                        first = item;
                    if (first != null && second == null)
                        second = item;

                    if (item.IsShorts)
                        continue;
                    if (item.VideoStatus != VideoStatus.Default)
                        continue;

                    if (item.PublishedTimeSeconds < TimeSeconds.Week)
                        videosByDay7.Add(item);
                    videosByDay30.Add(item);
                }

                if (first?.PublishedTimeSeconds >= TimeSeconds.Month * 3 &&
                    second?.PublishedTimeSeconds >= TimeSeconds.Month * 3)
                {
                    vtuber.Status = Status.NotActivity;
                    var _time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    Console.WriteLine($"[{_time}][{index}/{count}] Update vtuber not activity {vtuber.Name}");
                    await SleepRandom();
                    continue;
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
                var highestSingingDay7 = videosByDay7
                    .Where(it => 
                        it.Title.Contains("歌回") ||
                        it.Title.Contains("歌枠"))
                    .OrderByDescending(it => it.ViewCount)
                    .FirstOrDefault();
                var highestSingingDay30 = videosByDay30
                    .Where(it =>
                        it.Title.Contains("歌回") ||
                        it.Title.Contains("歌枠"))
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
                    HighestSingingViewCountDay7 = highestSingingDay7?.ViewCount ?? 0,
                    HighestViewSingingVideoUrlDay7 = highestSingingDay7?.ShortUrl ?? "",
                    HighestViewSingingVideoTitleDay7 = highestSingingDay7?.Title ?? "",
                    HighestViewSingingVideoThumbnailDay7 = highestSingingDay7?.Thumbnails?.LastOrDefault()?.Url ?? "",
                    MedianViewCountDay30 = medianViewCountDay30,
                    HighestViewCountDay30 = highestDay30?.ViewCount ?? 0,
                    HighestViewVideoUrlDay30 = highestDay30?.ShortUrl ?? "",
                    HighestViewVideoTitleDay30 = highestDay30?.Title ?? "",
                    HighestViewVideoThumbnailDay30 = highestDay30?.Thumbnails?.LastOrDefault()?.Url ?? "",
                    HighestSingingViewCountDay30 = highestSingingDay30?.ViewCount ?? 0,
                    HighestViewSingingVideoUrlDay30 = highestSingingDay30?.ShortUrl ?? "",
                    HighestViewSingingVideoTitleDay30 = highestSingingDay30?.Title ?? "",
                    HighestViewSingingVideoThumbnailDay30 = highestSingingDay30?.Thumbnails?.LastOrDefault()?.Url ?? "",
                };
                _db.Datas.Create(data);

                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"[{time}][{index}/{count}] Create data {vtuber.Name}");
                await SleepRandom();
            }
        }
    }
}
