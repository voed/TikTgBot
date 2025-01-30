using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;

namespace TikTgBot.Services;

public class TiktokIdlService(Configuration configuration, ILogger<TiktokIdlService> logger) : IDlService
{
    private static HttpClientHandler HttpClientHandler = new()
    {
        AllowAutoRedirect = true
    };

    private readonly HttpClient _httpClient = new(HttpClientHandler);

    //language=regexp
    private const string TiktokHtmlPattern = """
                                             \"playAddr\"\:\"(?'url'[A-Za-z0-9\:\\\-\.\?\=\&\%_]+)"
                                             """;


    //private string _apiUrl = "https://api22-normal-c-useast2a.tiktokv.com";
    public async Task<byte[]?> GetVideo(string url, ServiceType serviceType, CancellationTokenSource cts)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(configuration.UserAgent);
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