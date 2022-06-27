using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VtuberData.Models
{
    public class Data
    {
        public int Id { get; set; }
        public string ChannelUrl { get; set; } = "";
        public string Name { get; set; } = "";
        public long SubscriberCount { get; set; }
        public long ViewCount { get; set; }
        public long MedianViewCountDay7 { get; set; }
        public long HighestViewCountDay7 { get; set; }
        public string HighestViewVideoUrlDay7 { get; set; } = "";
        public string HighestViewVideoTitleDay7 { get; set; } = "";
        public string HighestViewVideoThumbnailDay7 { get; set; } = "";
        public string HighestViewVideoRichThumbnailDay7 { get; set; } = "";
        public long MedianViewCountDay30 { get; set; }
        public long HighestViewCountDay30 { get; set; }
        public string HighestViewVideoUrlDay30 { get; set; } = "";
        public string HighestViewVideoTitleDay30 { get; set; } = "";
        public string HighestViewVideoThumbnailDay30 { get; set; } = "";
        public string HighestViewVideoRichThumbnailDay30 { get; set; } = "";
    }
}
