using BotFramework;
using BotFramework.Attributes;
using BotFramework.Enums;
using BotFramework.Setup;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TikTgBot;

public class EventHandler:BotEventHandler
{
    private const string FullPattern = @".*?tiktok.com/.*?(/video/(?'video'\d{19}))|(?'vm'.*?vm.)tiktok.com/.{9}(.*?)";

    private readonly HttpClient _httpClient;
    private readonly string ApiUrl = "https://api16-normal-c-useast1a.tiktokv.com/aweme/v1/feed/?aweme_id=";
    private readonly Configuration _configuration;

    public EventHandler(Configuration configuration)
    {
        var handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false
        };
        _httpClient = new HttpClient(handler);
        _configuration = configuration;
    }

    [HandleCondition(ConditionType.All)]
    [Message(MessageFlag.HasEntity)]
    [RegexTextMessage(FullPattern, TextContent.Text )]
    public async Task FindTiktok()
    {
        if (!_configuration.Chats.Contains(Chat.Id))
            return;

        var num = 0;
        
        foreach (var ent in RawUpdate.Message.Entities)
        {

            if (ent.Type == MessageEntityType.Url)
            {
                var url = await GetVideoLink(RawUpdate.Message.EntityValues.ElementAt(num));
                if (!string.IsNullOrWhiteSpace(url))
                {
                    await Bot.SendChatActionAsync(Chat.Id, ChatAction.UploadVideo);
                    await Bot.SendVideoAsync(Chat.Id, new InputFileUrl(url), replyToMessageId: RawUpdate.Message.MessageId);
                }

            }
            num++;
        }
    }

    private async Task<string?> GetVideoLink(string url)
    {

        var match = Regex.Matches(url, FullPattern).FirstOrDefault();
        if (match == null) 
            return null;

        try
        {
            var isVm = match.Groups.Values.Any(x => x is { Success: true, Name: "vm" });
            if (isVm)// vm.tiktok.com/... link requires to get redirect url first
            {
                var resp = await _httpClient.GetAsync(url);
                return await GetVideoLink(resp.Headers.Location?.AbsoluteUri);
            }
            else
            {
                var videoId = match.Groups.Values.FirstOrDefault(x => x.Name == "video")?.Value;
                var resp = await _httpClient.GetAsync($"{ApiUrl}{videoId}");
                IEnumerable<dynamic> jobj = JObject.Parse(await resp.Content.ReadAsStringAsync())["aweme_list"] ?? throw new Exception();
                var aweme = jobj?.FirstOrDefault(x => x.aweme_id == videoId && x.video.duration > 0) ?? throw new Exception();

                return (string)(aweme["video"]["play_addr"]["url_list"].First);
            }
        }
        catch (Exception)
        {
            return null;
        }
    }
}