namespace TikTgBot;

public class Configuration
{
    public List<long> Chats { get; set; } = [];
    public string ApiUrl { get; set; } = "https://api22-normal-c-useast2a.tiktokv.com";
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36";

    public Cookies Cookies { get; set; } = new();
}

public class Cookies
{
    public string Instagram { get; set; } = "instagram_cookies.txt";
    public string Youtube { get; set; } = "youtube_cookies.txt";
    public string TikTok { get; set; } = "tiktok_cookies.txt";
}