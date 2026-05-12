using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;

namespace TikTgBot.Services;

public class TiktokDlService(Configuration configuration, ILogger<TiktokDlService> logger) : IDlService
{
    private static HttpClientHandler HttpClientHandler = new()
    {
        AllowAutoRedirect = true
    };

    private readonly HttpClient _httpClient = new(HttpClientHandler);

    //language=regexp
    private const string TiktokHtmlPattern = """playAddr"\s*:\s*"(?'url'[^"]+)""";


    //private string _apiUrl = "https://api22-normal-c-useast2a.tiktokv.com";
    async Task<byte[]?> IDlService.GetVideo<T>(string url, ServiceType serviceType, CancellationTokenSource cts)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(configuration.UserAgent);
            _httpClient.DefaultRequestHeaders.Add("UserAgent", configuration.UserAgent);
            logger.LogInformation("Tiktok link: {url}", url);
            var body = await _httpClient.GetStringAsync(url);
            var match = Regex.Match(body, TiktokHtmlPattern);
            var videoUrl = match.Groups["url"].Value;
            videoUrl = videoUrl.Replace(@"\u002F", "/");
            logger.LogInformation("DL link: {dl_url}", videoUrl);

            return await _httpClient.GetByteArrayAsync(HttpUtility.UrlDecode(videoUrl));
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
            return null;
        }
    }


}