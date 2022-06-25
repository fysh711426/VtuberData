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
using YoutubeParser.Models;

namespace VtuberData.Crawlers
{
    public class VtuberCrawler : BaseCrawler
    {
        private int _id = 1;
        private string _now = "";
        private string _path = "";
        private Dictionary<string, Vtuber> _recordDict = new Dictionary<string, Vtuber>();
        private CsvConfiguration _configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            ShouldQuote = (args) => true
        };

        public async Task Load(string path)
        {
            _id = 1;
            _now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _path = path;
            _recordDict = new Dictionary<string, Vtuber>();

            if (File.Exists(_path))
            {
                using (var reader = new StreamReader(_path, new UTF8Encoding(true)))
                using (var csv = new CsvReader(reader, _configuration))
                {
                    var records = await csv.GetRecordsExAsync<Vtuber>();
                    _recordDict = records
                        .ToDictionary(it => it.ChannelUrl);
                }
            }
        }

        public async Task Save()
        {
            using (var writer = new StreamWriter(_path, false, new UTF8Encoding(true)))
            using (var csv = new CsvWriter(writer, _configuration))
            {
                csv.WriteHeader<Vtuber>();
                csv.NextRecord();

                if (_recordDict.Count > 0)
                {
                    var order = _recordDict
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
            Console.WriteLine($"[{_time}] Save data success. @ {str}");
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
                var vtubers = MapVtuber(html);

                Status getStatus(string status)
                {
                    if (status == "準備中")
                        return Status.Prepare;
                    if (status == "停止活動")
                        return Status.Graduate;
                    return Status.Activity;
                }

                foreach (var item in vtubers)
                {
                    if (item.youtubeUrl == "")
                        continue;

                    var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var channelId = item.youtubeUrl.Replace("https://www.youtube.com/channel/", "");
                    var youtubeChannel = new YoutubeChannel(channelId);
                    var info = null as Info;
                    try
                    {
                        info = await youtubeChannel.GetInfoAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error] {channelId}");
                        Console.WriteLine(ex.Message);
                        await SleepRandom();
                        continue;
                    }
                    var model = null as Vtuber;
                    var status = getStatus(item.status);
                    if (!_recordDict.ContainsKey(item.youtubeUrl))
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
                        _recordDict.Add(item.youtubeUrl, model);
                        Console.WriteLine($"[{time}] Create vtuber {model.Name}");
                    }
                    else
                    {
                        model = _recordDict[item.youtubeUrl];
                        if (model.Status != Status.Closure)
                        {
                            //if (item.name != "")
                            //    model.Name = item.name;
                            if (info.Title != "")
                                model.ChannelName = info.Title;
                            if (info.Thumbnails.Count > 0)
                                model.Thumbnail = info.Thumbnails.LastOrDefault()?.Url ?? "";
                            model.Area = "TW";
                            model.Status = status;
                            Console.WriteLine($"[{time}] Update vtuber {model.Name}");
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
                        if (!_recordDict.ContainsKey(item.youtubeUrl))
                            continue;

                        var model = _recordDict[item.youtubeUrl];
                        if (model.Id < 0)
                            if (model.Status != Status.Closure)
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
                        var _url = $"https://virtual-youtuber.userlocal.jp/schedules/new?youtube={item.userId}";
                        using var _response = await clinet.GetAsync(_url);
                        var _html = await _response.Content.ReadAsStringAsync();

                        var youtubeUrl = Regex.Match(_html, @"<input size=""64.*?value=""(https:\/\/www\.youtube\.com\/channel\/.*?)"" name=""live_schedule")
                            .Groups[1].Value.Trim();
                        if (youtubeUrl == "")
                            continue;
                        _cacheChannelUrl[item.userId] = youtubeUrl;

                        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        var channelId = youtubeUrl.Replace("https://www.youtube.com/channel/", "");
                        var youtubeChannel = new YoutubeChannel(channelId);
                        var info = null as Info;
                        try
                        {
                            info = await youtubeChannel.GetInfoAsync();
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine($"[Error] {channelId}");
                            Console.WriteLine(ex.Message);
                            await SleepRandom();
                            continue;
                        }
                        var model = null as Vtuber;
                        if (!_recordDict.ContainsKey(youtubeUrl))
                        {
                            model = new Vtuber();
                            model.Id = (_id++) * -1;
                            model.ChannelUrl = youtubeUrl;
                            model.Name = item.name;
                            model.Status = Status.Activity;
                            model.CreateTime = _now;
                            model.ChannelName = info.Title;
                            model.Thumbnail = info.Thumbnails.LastOrDefault()?.Url ?? "";
                            _recordDict.Add(youtubeUrl, model);
                            Console.WriteLine($"[{time}] Create vtuber {model.Name}");
                        }
                        else
                        {
                            model = _recordDict[youtubeUrl];
                            if (model.Status != Status.Closure)
                            {
                                //if (item.name != "")
                                //    model.Name = item.name;
                                if (info.Title != "")
                                    model.ChannelName = info.Title;
                                if (info.Thumbnails.Count > 0)
                                    model.Thumbnail = info.Thumbnails.LastOrDefault()?.Url ?? "";
                                Console.WriteLine($"[{time}] Update vtuber {model.Name}");
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
                        if (!_recordDict.ContainsKey(youtubeUrl))
                            continue;

                        var model = _recordDict[youtubeUrl];
                        if (model.Id < 0)
                            if (model.Status != Status.Closure)
                            {
                                model.Company = "Hololive";
                                model.Group = getGroup(item.group);
                                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                Console.WriteLine($"[{time}] Update area {model.Name}");
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
                            if (!_recordDict.ContainsKey(youtubeUrl))
                                continue;

                            var model = _recordDict[youtubeUrl];
                            if (model.Id < 0)
                                if (model.Status != Status.Closure)
                                {
                                    model.Company = "彩虹社";
                                    model.Group = getGroup(item.group);
                                    var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    Console.WriteLine($"[{time}] Update area {model.Name}");
                                }
                            await SleepRandom();
                        }
                    }
                }
            }
        }
    }
}
