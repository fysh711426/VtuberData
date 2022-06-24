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

namespace VtuberData.Crawlers
{
    public class VtuberCrawler : BaseCrawler
    {
        public async Task CreateOrUpdateVtubers(string directory)
        {
            var recordDict = new Dictionary<string, Vtuber>();
            var csvPath = Path.Combine(directory, "Vtubers.csv");
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Encoding = Encoding.UTF8,
                HasHeaderRecord = true,
                ShouldQuote = (args) => true
            };

            if (File.Exists(csvPath))
            {
                using (var reader = new StreamReader(csvPath))
                using (var csv = new CsvReader(reader, configuration))
                {
                    recordDict = csv.GetRecords<Vtuber>()
                        .ToDictionary(it => it.ChannelUrl);
                }
            }
            
            // 更新 Vtuber 資料
            var clinet = _httpClient;
            var url = "https://vt.cdein.cc/list/?a=a&o=D";
            var response = await clinet.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();
            var vtubers = html
                .Pipe(it => Regex.Match(it, @"<section.*?>([\s\S]*?)<\/section>"))
                .Select(m => m.Groups[1].Value)
                .Pipe(it => Regex.Matches(it, @"<div class=""d-flex"">([\s\S]*?)<span class=""position-absolute py-1"">(.*?)<\/span>"))
                .SelectMany(m => m.Value)
                .Pipes(it => it
                    .Pipe(itt => Regex.Match(itt, @"href=""(.*?)""[\s\S]*?src=""(.*?)""[\s\S]*?person-name"">(.*?)<\/a>[\s\S]*?person-abbr"">(.*?)<\/div>[\s\S]*?href=""(https:\/\/www\.youtube.*?)""[\s\S]*?py-1"">(.*?)<\/span>"))
                    .Select<(string url, string thumb, string name, string abbr, string youtubeUrl, string status)>(itt =>
                        (itt.Groups[1].Value,
                         itt.Groups[2].Value,
                         itt.Groups[3].Value,
                         itt.Groups[4].Value,
                         itt.Groups[5].Value, 
                         itt.Groups[6].Value)));

            Status getStatus(string status)
            {
                if (status == "準備中")
                    return Status.Prepare;
                if (status == "停止活動")
                    return Status.Graduate;
                return Status.Activity;
            }

            var id = 1;
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            foreach (var item in vtubers)
            {
                if (id == 10)
                    break;
                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var channelId = item.youtubeUrl.Replace("https://www.youtube.com/channel/", "");
                var youtubeChannel = new YoutubeChannel(channelId);
                var info = await youtubeChannel.GetInfoAsync();
                var model = null as Vtuber;
                var status = getStatus(item.status);
                if (!recordDict.ContainsKey(item.youtubeUrl))
                {
                    model = new Vtuber();
                    model.Id = (id++) * -1;
                    model.ChannelUrl = item.youtubeUrl;
                    model.Name = item.name;
                    model.Area = "TW";
                    model.Status = status;
                    model.CreateTime = now;
                    model.ChannelName = info.Title;
                    model.Thumbnail = info.Thumbnails.LastOrDefault()?.Url ?? "";
                    recordDict.Add(item.youtubeUrl, model);
                    Console.WriteLine($"[{time}] Create vtuber {model.Name}.");
                }
                else
                {
                    model = recordDict[item.youtubeUrl];
                    if (model.Status != Status.Closure)
                    {
                        if (item.name != "")
                            model.Name = item.name;
                        if (info.Title != "")
                            model.ChannelName = info.Title;
                        if (info.Thumbnails.Count > 0)
                            model.Thumbnail = info.Thumbnails.LastOrDefault()?.Url ?? "";
                        model.Status = status;
                        Console.WriteLine($"[{time}] Update vtuber {model.Name}.");
                    }
                }
                await SleepRandom(500, 1500);
            }

            using (var writer = new StreamWriter(csvPath))
            using (var csv = new CsvWriter(writer, configuration))
            {
                csv.WriteHeader<Vtuber>();
                csv.NextRecord();

                if (recordDict.Count > 0)
                {
                    var order = recordDict
                        .ToList()
                        .Select(it => it.Value)
                        .OrderBy(it => it.Id)
                        .ToList();
                    var maxId = order.Max(it => it.Id);
                    foreach (var item in order)
                    {
                        if (item.Id < 0)
                            item.Id = maxId + item.Id * -1;
                    }
                    await csv.WriteRecordsAsync(order);
                }
            }

            var _time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine($"[{_time}] CreateOrUpdateVtubers success.");
        }
    }
}
