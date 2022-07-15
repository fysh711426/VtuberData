namespace VtuberData.Models
{
    public class Data
    {
        // 編號
        public int Id { get; set; }

        // 頻道網址
        public string ChannelUrl { get; set; } = "";
        
        // 名稱
        public string Name { get; set; } = "";
        
        // 訂閱數
        public long SubscriberCount { get; set; }
        
        // 觀看數
        public long ViewCount { get; set; }
        
        // 近7天影片觀看中位數
        public long MedianViewCountDay7 { get; set; }
        
        // 近7天影片最高觀看數
        public long HighestViewCountDay7 { get; set; }

        // 近7天觀看數最高的影片網址
        public string HighestViewVideoUrlDay7 { get; set; } = "";

        // 近7天觀看數最高的影片標題
        public string HighestViewVideoTitleDay7 { get; set; } = "";

        // 近7天觀看數最高的影片縮圖
        public string HighestViewVideoThumbnailDay7 { get; set; } = "";

        // 近7天歌回影片最高觀看數
        public long HighestSingingViewCountDay7 { get; set; }

        // 近7天觀看數最高的歌回影片網址
        public string HighestViewSingingVideoUrlDay7 { get; set; } = "";

        // 近7天觀看數最高的歌回影片標題
        public string HighestViewSingingVideoTitleDay7 { get; set; } = "";

        // 近7天觀看數最高的歌回影片縮圖
        public string HighestViewSingingVideoThumbnailDay7 { get; set; } = "";
        
        // 近30天影片觀看中位數
        public long MedianViewCountDay30 { get; set; }

        // 近30天影片最高觀看數
        public long HighestViewCountDay30 { get; set; }

        // 近30天觀看數最高的影片網址
        public string HighestViewVideoUrlDay30 { get; set; } = "";

        // 近30天觀看數最高的影片標題
        public string HighestViewVideoTitleDay30 { get; set; } = "";

        // 近30天觀看數最高的影片縮圖
        public string HighestViewVideoThumbnailDay30 { get; set; } = "";

        // 近30天歌回影片最高觀看數
        public long HighestSingingViewCountDay30 { get; set; }

        // 近30天觀看數最高的歌回影片網址
        public string HighestViewSingingVideoUrlDay30 { get; set; } = "";

        // 近30天觀看數最高的歌回影片標題
        public string HighestViewSingingVideoTitleDay30 { get; set; } = "";

        // 近30天觀看數最高的歌回影片縮圖
        public string HighestViewSingingVideoThumbnailDay30 { get; set; } = "";
    }
}
