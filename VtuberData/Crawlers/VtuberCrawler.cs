using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VtuberData.Extensions;
using VtuberData.Models;
using YoutubeParser;
using YoutubeParser.Channels;
using YoutubeParser.Models;

namespace VtuberData.Crawlers
{
    public class VtuberCrawler : BaseCrawler
    {
        private int _id = 1;
        private string _now = "";
        private Dictionary<string, Vtuber> _vtuberDict = new Dictionary<string, Vtuber>();
        private CsvConfiguration _configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            ShouldQuote = (args) => true
        };

        public async Task Load(string path)
        {
            _id = 1;
            _now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _vtuberDict = new Dictionary<string, Vtuber>();

            if (File.Exists(path))
            {
                using (var reader = new StreamReader(path, new UTF8Encoding(true)))
                using (var csv = new CsvReader(reader, _configuration))
                {
                    var records = await csv.GetAllRecordsAsync<Vtuber>();
                    _vtuberDict = records
                        .ToDictionary(it => it.ChannelUrl);
                }
            }
        }

        public async Task Save(string path)
        {
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            using (var csv = new CsvWriter(writer, _configuration))
            {
                csv.WriteHeader<Vtuber>();
                csv.NextRecord();

                if (_vtuberDict.Count > 0)
                {
                    var order = _vtuberDict
                        .Select(it => it.Value)
                        .ToList();
                    var maxId = order
                        .Where(it => it.Id > 0)
                        .OrderByDescending(it => it.Id)
                        .FirstOrDefault()?.Id ?? 0;
                    foreach (var item in order)
                    {
                        if (item.Id < 0)
                            item.Id = maxId + item.Id * -1;
                    }
                    await csv.WriteRecordsAsync(
                        order.OrderBy(it => it.Id));
                }
            }
            var _time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var ts = DateTime.Parse(_time) - DateTime.Parse(_now);
            var str = (ts.Hours.ToString("00") == "00" ? "" : ts.Hours.ToString("00") + "h") + ts.Minutes.ToString("00") + "m" + ts.Seconds.ToString("00") + "s";
            Console.WriteLine($"[{_time}] Save vtubers success. @ {str}");
        }

        public async Task CreateOrUpdateVtubersTw()
        {
            IEnumerable<(string url, string thumb, string name, string abbr, string youtubeUrl, string status)>
                MapVtuber(string html)
            {
                return html
                    .Pipe(it => Regex.Match(it, @"<section.*?>([\s\S]*?)<\/section>"))
                    .Select(m => m.Groups[1].Value)
                    .Pipe(it => Regex.Matches(it, @"<div class=""d-flex"">([\s\S]*?)<span class=""position-absolute py-1"">(.*?)<\/span>"))
                    .SelectMany(m => m.Value)
                    .Pipes(it => it
                        .Pipe(itt => Regex.Match(itt, @"href=""([\s\S]*?)""[\s\S]*?src=""([\s\S]*?)""[\s\S]*?person-name"">([\s\S]*?)<\/a>[\s\S]*?person-abbr"">([\s\S]*?)<\/div>([\s\S]*?)py-1"">([\s\S]*?)<\/span>"))
                        .Select<(string url, string thumb, string name, string abbr, string youtubeUrl, string status)>(m =>
                            (m.Groups[1].Value.Trim(),
                             m.Groups[2].Value.Trim(),
                             m.Groups[3].Value.Trim(),
                             m.Groups[4].Value.Trim(),
                             m.Groups[5].Value
                                .Pipe(ittt => Regex.Match(ittt, @"href=""(https:\/\/www\.youtube.*?)"""))
                                .Select(m => m.Groups[1].Value),
                             m.Groups[6].Value.Trim())));
            }

            // Update Vtuber Data
            {
                var clinet = _httpClient;
                var url = "https://vt.cdein.cc/list/?a=a&o=D";
                using var response = await clinet.GetAsync(url);
                var html = await response.Content.ReadAsStringAsync();
                var vtubers = MapVtuber(html).ToList();

                Status getStatus(string status)
                {
                    if (status == "準備中")
                        return Status.Prepare;
                    if (status == "停止活動")
                        return Status.Graduate;
                    return Status.Activity;
                }

                var index = 0;
                var count = vtubers.Count;
                foreach (var item in vtubers)
                {
                    index++;
                    if (item.youtubeUrl == "")
                        continue;

                    var model = _vtuberDict.ContainsKey(item.youtubeUrl)
                        ? _vtuberDict[item.youtubeUrl] : null;
                    if (model?.Status != Status.Prepare &&
                        model?.Status != Status.Activity)
                        continue;

                    var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var channelId = item.youtubeUrl.Replace("https://www.youtube.com/channel/", "");
                    var youtube = new YoutubeClient();
                    var info = null as Channel;
                    try
                    {
                        info = await youtube.Channel.GetAsync(channelId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error] {channelId}");
                        Console.WriteLine(ex.Message);
                        await SleepRandom();
                        continue;
                    }
                    var status = getStatus(item.status);
                    if (info.Title == "")
                        status = Status.Graduate;
                    if (model == null)
                    {
                        model = new Vtuber();
                        model.Id = (_id++) * -1;
                        model.ChannelUrl = item.youtubeUrl;
                        model.Name = item.name;
                        model.Area = "TW";
                        model.Status = status;
                        model.CreateTime = _now;
                        model.ChannelName = info.Title;
                        model.Thumbnail = info.Thumbnails.LastOrDefault()?.Url ?? "";
                        _vtuberDict.Add(item.youtubeUrl, model);
                        Console.WriteLine($"[{time}][{index}/{count}] Create tw vtuber {model.Name}");
                    }
                    else
                    {
                        if (model.Status == Status.Prepare ||
                            model.Status == Status.Activity)
                        {
                            // If the status changed from prepare to activity, update the time.
                            if (status == Status.Activity && model.Status == Status.Prepare)
                                model.CreateTime = _now;
                            //if (item.name != "")
                            //    model.Name = item.name;
                            if (info.Title != "")
                                model.ChannelName = info.Title;
                            if (info.Thumbnails.Count > 0)
                                model.Thumbnail = info.Thumbnails.LastOrDefault()?.Url ?? "";
                            model.Area = "TW";
                            model.Status = status;
                            Console.WriteLine($"[{time}][{index}/{count}] Update tw vtuber {model.Name}");
                        }
                    }
                    await SleepRandom();
                }
            }

            // Update Area Data
            {
                var tags = new List<(string name, string area)>
                {
                    ("香港", "HK"),
                    ("馬來西亞", "MY")
                };
                foreach (var tag in tags)
                {
                    var clinet = _httpClient;
                    var url = $"https://vt.cdein.cc/tag/{tag.name}";
                    using var response = await clinet.GetAsync(url);
                    var html = await response.Content.ReadAsStringAsync();
                    var vtubers = MapVtuber(html);
                    foreach (var item in vtubers)
                    {
                        if (item.youtubeUrl == "")
                            continue;
                        if (!_vtuberDict.ContainsKey(item.youtubeUrl))
                            continue;

                        var model = _vtuberDict[item.youtubeUrl];
                        if (model.Id < 0)
                        {
                            model.Area = tag.area;
                            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            Console.WriteLine($"[{time}] Update area {model.Name}");
                        }
                    }
                }
            }
        }

        private Dictionary<string, string> _cacheChannelUrl = new Dictionary<string, string>();
        public async Task CreateOrUpdateVtubersJp()
        {
            _cacheChannelUrl = new Dictionary<string, string>();

            IEnumerable<(string userId, string name, string group)>
                MapVtuber(string html)
            {
                return html
                    .Pipe(it => Regex.Match(it, @"<table([\s\S]*?)<script>"))
                    .Select(m => m.Groups[1].Value)
                    .Pipe(it => Regex.Matches(it, @"<\/strong>[\s\S]*?href=""\/user\/(.*?)"".*?>([\s\S]*?)<\/a>[\s\S]*?<a.*?>(.*?)<\/a>"))
                    .SelectMany<(string userId, string name, string group)>(m => (
                        m.Groups[1].Value.Trim(),
                        m.Groups[2].Value.Trim(),
                        m.Groups[3].Value.Trim()
                    ));
            }

            // Update Vtuber Data JP
            {
                var index = 0;
                var count = 2000;
                for (var i = 0; i < 40; i++)
                {
                    var clinet = _httpClient;
                    var url = $"https://virtual-youtuber.userlocal.jp/document/ranking?page={i + 1}";
                    using var response = await clinet.GetAsync(url);
                    var html = await response.Content.ReadAsStringAsync();
                    var vtubers = html
                        .Pipe(it => Regex.Match(it, @"<table([\s\S]*?)<script>"))
                        .Select(m => m.Groups[1].Value)
                        .Pipe(it => Regex.Matches(it, @"<\/strong>[\s\S]*?href=""\/user\/(.*?)"".*?>([\s\S]*?)<\/a>"))
                        .SelectMany<(string userId, string name)>(m => (
                            m.Groups[1].Value.Trim(),
                            m.Groups[2].Value.Trim()
                        ));

                    foreach (var item in vtubers)
                    {
                        index++;

                        var _url = $"https://virtual-youtuber.userlocal.jp/schedules/new?youtube={item.userId}";
                        using var _response = await clinet.GetAsync(_url);
                        var _html = await _response.Content.ReadAsStringAsync();

                        var youtubeUrl = Regex.Match(_html, @"<input size=""64.*?value=""(https:\/\/www\.youtube\.com\/channel\/.*?)"" name=""live_schedule")
                            .Groups[1].Value.Trim();
                        if (youtubeUrl == "")
                            continue;
                        var model = _vtuberDict.ContainsKey(youtubeUrl)
                            ? _vtuberDict[youtubeUrl] : null;
                        if (model?.Status != Status.Prepare &&
                            model?.Status != Status.Activity)
                            continue;
                        _cacheChannelUrl[item.userId] = youtubeUrl;

                        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        var channelId = youtubeUrl.Replace("https://www.youtube.com/channel/", "");
                        var youtube = new YoutubeClient();
                        var info = null as Channel;
                        try
                        {
                            info = await youtube.Channel.GetAsync(channelId);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Error] {channelId}");
                            Console.WriteLine(ex.Message);
                            await SleepRandom();
                            continue;
                        }
                        var status = Status.Activity;
                        if (info.Title == "")
                            status = Status.Graduate;
                        if (model == null)
                        {
                            model = new Vtuber();
                            model.Id = (_id++) * -1;
                            model.ChannelUrl = youtubeUrl;
                            model.Name = item.name;
                            model.Status = status;
                            model.CreateTime = _now;
                            model.ChannelName = info.Title;
                            model.Thumbnail = info.Thumbnails.LastOrDefault()?.Url ?? "";
                            _vtuberDict.Add(youtubeUrl, model);
                            Console.WriteLine($"[{time}][{index}/{count}] Create jp vtuber {model.Name}");
                        }
                        else
                        {
                            if (model.Status == Status.Prepare ||
                                model.Status == Status.Activity)
                            {
                                //if (item.name != "")
                                //    model.Name = item.name;
                                if (info.Title != "")
                                    model.ChannelName = info.Title;
                                if (info.Thumbnails.Count > 0)
                                    model.Thumbnail = info.Thumbnails.LastOrDefault()?.Url ?? "";
                                Console.WriteLine($"[{time}][{index}/{count}] Update jp vtuber {model.Name}");
                            }
                        }
                        await SleepRandom();
                    }
                }
            }

            // Update Company Data
            {
                // Hololive
                {
                    var clinet = _httpClient;
                    var url = $"https://virtual-youtuber.userlocal.jp/office/hololive_all";
                    using var response = await clinet.GetAsync(url);
                    var html = await response.Content.ReadAsStringAsync();
                    var vtubers = MapVtuber(html);

                    string getGroup(string group)
                    {
                        if (group == "ホロライブ")
                            return "JP";
                        if (group == "hololive English")
                            return "EN";
                        if (group == "hololive Indonesia")
                            return "ID";
                        if (group == "ホロスターズ")
                            return "Stars";
                        return "";
                    }

                    foreach (var item in vtubers)
                    {
                        if (!_cacheChannelUrl.ContainsKey(item.userId))
                            continue;
                        var youtubeUrl = _cacheChannelUrl[item.userId];
                        if (!_vtuberDict.ContainsKey(youtubeUrl))
                            continue;

                        var model = _vtuberDict[youtubeUrl];
                        if (model.Id < 0)
                        {
                            model.Company = "Hololive";
                            model.Group = getGroup(item.group);
                            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            Console.WriteLine($"[{time}] Update company {model.Name}");
                        }
                        await SleepRandom();
                    }
                }

                // NIJISANJI
                {
                    var urls = new string[]
                    {
                        "https://virtual-youtuber.userlocal.jp/office/nijisanji_all",
                        "https://virtual-youtuber.userlocal.jp/office/nijisanji_world"
                    };

                    string getGroup(string group)
                    {
                        if (group == "にじさんじ(1・2期生)")
                            return "1、2 期生";
                        if (group == "にじさんじ(統合後)")
                            return "統合後";
                        if (group == "にじさんじ(ゲーマーズ出身)")
                            return "Gamers";
                        if (group == "にじさんじ(SEEDs出身)")
                            return "SEEDs";
                        if (group == "NIJISANJI EN")
                            return "EN";
                        if (group == "NIJISANJI ID")
                            return "ID";
                        if (group == "NIJISANJI KR")
                            return "KR";
                        return "";
                    }

                    foreach (var url in urls)
                    {
                        var clinet = _httpClient;
                        using var response = await clinet.GetAsync(url);
                        var html = await response.Content.ReadAsStringAsync();
                        var vtubers = MapVtuber(html);

                        foreach (var item in vtubers)
                        {
                            if (!_cacheChannelUrl.ContainsKey(item.userId))
                                continue;
                            var youtubeUrl = _cacheChannelUrl[item.userId];
                            if (!_vtuberDict.ContainsKey(youtubeUrl))
                                continue;

                            var model = _vtuberDict[youtubeUrl];
                            if (model.Id < 0)
                            {
                                model.Company = "彩虹社";
                                model.Group = getGroup(item.group);
                                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                Console.WriteLine($"[{time}] Update company {model.Name}");
                            }
                            await SleepRandom();
                        }
                    }
                }
            }
        }
    }
}
