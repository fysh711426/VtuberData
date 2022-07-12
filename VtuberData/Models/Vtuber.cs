namespace VtuberData.Models
{
    public enum Status
    {
        Prepare,
        Activity,
        NotActivity,
        Graduate,
        Closure
    }

    public class Vtuber
    {
        public int Id { get; set; }
        public string ChannelUrl { get; set; } = "";
        public Status Status { get; set; }
        public string Name { get; set; } = "";
        public string Area { get; set; } = "";
        public string Company { get; set; } = "";
        public string Group { get; set; } = "";
        public string ChannelName { get; set; } = "";
        public string Thumbnail { get; set; } = "";
        public string CreateTime { get; set; } = "";
        public bool IsGreen { get; set; }
    }
}
