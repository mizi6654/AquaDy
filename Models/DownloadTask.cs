namespace AquaDy.Models
{
    /// <summary>
    /// 下载任务状态（避免与 System.Threading.Tasks.TaskStatus 冲突）
    /// </summary>
    public enum DownloadTaskStatus
    {
        Waiting,        // 等待中
        Parsing,        // 解析中
        Downloading,    // 下载中
        Processing,     // 处理中
        Paused,         // 已暂停
        Completed,      // 已完成
        Failed,         // 失败
        Cancelled       // 已取消
    }

    public enum ContentType
    {
        Video,
        Image,          // 图集 / 图文
        LivePhoto,      // 实况
        Mix,            // 合集
        Account,        // 账号主页
        Live            // 直播
    }

    public class DownloadTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = "未知标题";
        public string Author { get; set; } = "";
        public string Url { get; set; } = "";
        public string CoverUrl { get; set; } = "";
        /// <summary>配乐 BGM 地址</summary>
        public string MusicUrl { get; set; } = "";
        /// <summary>所属合集名（合集下载时）</summary>
        public string MixName { get; set; } = "";
        public ContentType Type { get; set; } = ContentType.Video;
        public DownloadTaskStatus Status { get; set; } = DownloadTaskStatus.Waiting;
        public double Progress { get; set; } = 0;
        public string Speed { get; set; } = "-";
        public string Remaining { get; set; } = "-";
        public string Size { get; set; } = "-";
        public string SavePath { get; set; } = "";
        public DateTime CreateTime { get; set; } = DateTime.Now;
        /// <summary>作品发布时间（解析自 create_time）</summary>
        public DateTime? PublishTime { get; set; } = null;
        public string PublishTimeText => PublishTime.HasValue ? PublishTime.Value.ToString("yyyy-MM-dd HH:mm") : "-";
        public string ErrorMessage { get; set; } = "";
        public string AwemeId { get; set; } = "";
        public int Quality { get; set; } = 0; // 0=最高
        public bool DownloadCover { get; set; } = false;
        public bool DownloadAudio { get; set; } = false;
        /// <summary>无水印视频/图集资源地址（解析后填充）</summary>
        public string DownloadUrl { get; set; } = "";
        /// <summary>图集时多图地址</summary>
        public System.Collections.Generic.List<string> ImageUrls { get; set; } = new();
        public string Description { get; set; } = "";
    }
}
