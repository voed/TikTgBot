using BotFramework;
using BotFramework.Attributes;
using BotFramework.Enums;
using BotFramework.Setup;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Net.Http;
using System.Web;
using ByteSizeLib;
using Telegram.Bot.Types.InlineQueryResults;

namespace TikTgBot;

public class EventHandler:BotEventHandler
{
    //language=regexp
    private const string FullPattern = @"(https://)?(www\.)?(tiktok.com/.*?(/video/(?'video'\d{19}))|m\.tiktok\.com/v/(?'video'\d{19})\.html.*?|(?'vm'.*?(vm\.tiktok\.com/)|(tiktok\.com/t/)).{9}(.*?).*?)";
    //language=regexp
    private const string YoutubePattern = @"(https://)?(www\.)?youtube\.com/shorts/(?'video'\w{11}).*?";
    //language=regexp
    private const string TiktokHtmlPattern = """
                                             \"playAddr\"\:\"(?<url>[A-Za-z0-9\:\\\-\.\?\=\&\%_]+)"
                                             """;

    private readonly HttpClient _httpClient;
    private string _apiUrl = "https://api22-normal-c-useast2a.tiktokv.com";
    private readonly Configuration _configuration;
    private readonly ILogger _logger;

    public EventHandler(Configuration configuration, ILogger<EventHandler> logger)
    {
        _logger = logger;
        var handler = new HttpClientHandler()
        {
            AllowAutoRedirect = true
        };
        _httpClient = new HttpClient(handler);
        _configuration = configuration;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_configuration.UserAgent);
        _apiUrl = $"{_configuration.ApiUrl}/aweme/v1/feed/";

    }


    [HandleCondition(ConditionType.All)]
    [Message(MessageFlag.HasEntity)]
    [RegexTextMessage(FullPattern, TextContent.Text )]
    public async Task FindTiktok()
    {
        if (!_configuration.Chats.Contains(Chat.Id))
            return;

        var num = 0;
        var entList = new List<string>();
        foreach (var ent in RawUpdate.Message.Entities)
        {

            if (ent.Type == MessageEntityType.Url)
            {
                var url = RawUpdate.Message.EntityValues.ElementAt(num);
                var stream = await GetVideo(url);

                if (stream != null)
                {
                    if(entList.Contains(url))
                        continue;
                    entList.Add(RawUpdate.Message.EntityValues.ElementAt(num));
                    var size = ByteSize.FromBytes(stream.Length);
                    _logger.LogInformation("File size is {size}mb", size.MegaBytes);
                    if (size.MegaBytes > 50)
                        continue;
                    await Bot.SendChatActionAsync(Chat.Id, ChatAction.UploadVideo);
                    var ms = new MemoryStream(stream);
                    await Bot.SendVideoAsync(Chat.Id, new InputFile(ms), replyToMessageId: RawUpdate.Message.MessageId);
                    await ms.DisposeAsync();

                }

            }
            num++;
        }
    }


    private async Task<byte[]?> GetVideo(string url)
    {

        var match = Regex.Matches(url, FullPattern).FirstOrDefault();
        if (match == null)
        {
            _logger.LogWarning("Unable to parse tiktok link: {url}", url);
            return null;
        }


        try
        {
            _logger.LogInformation("Tiktok link: {url}", url);
            //var videoId = match.Groups.Values.FirstOrDefault(x => x.Name == "video")?.Value;
            var body = await _httpClient.GetStringAsync(url);
            var dl_match = Regex.Match(body, TiktokHtmlPattern);
            var dl_url = dl_match.Groups["url"].Value;
            dl_url = dl_url.Replace(@"\u002F", "/");
            _logger.LogInformation("DL link: {dl_url}", dl_url);

            return await _httpClient.GetByteArrayAsync(HttpUtility.UrlDecode(dl_url));
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return null;
        }
    }
}