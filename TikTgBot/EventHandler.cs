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
using TikTgBot.Services;

namespace TikTgBot;

public class EventHandler(Configuration configuration, ILogger<EventHandler> logger,
    YtDlpIdlService ytDlpIdlService, TiktokIdlService tiktokIdlService) : BotEventHandler
{

    //language=regexp
    private const string TiktokPattern = @"(https://)?(www\.)?(tiktok.com/.*?(/video/(?'video'\d{19}))|m\.tiktok\.com/v/(?'video'\d{19})\.html.*?|(?'vm'.*?(v[tm]\.tiktok\.com/)|(tiktok\.com/t/)).{9}(.*?).*?)";
    //language=regexp
    private const string YoutubePattern = @"(https://)?(www\.)?youtube\.com/shorts/(?'video'[\w-_]{11}).*?";
    //language=regexp
    private const string InstaPattern = @"(https://)?(www\.)?instagram\.com/reel/(?'video'[\w-_]{11}).*?";


    [HandleCondition(ConditionType.All)]
    [Message(MessageFlag.HasEntity)]
    [RegexTextMessage(InstaPattern, TextContent.Text)]
    public async Task FindReels()
    {
        await ProcessVideo(ServiceType.Instagram);
    }

    [HandleCondition(ConditionType.All)]
    [Message(MessageFlag.HasEntity)]
    [RegexTextMessage(TiktokPattern, TextContent.Text )]
    public async Task FindTiktok()
    {
        await ProcessVideo(ServiceType.TikTok);
    }

    [HandleCondition(ConditionType.All)]
    [Message(MessageFlag.HasEntity)]
    [RegexTextMessage(YoutubePattern, TextContent.Text)]
    public async Task FindYTDlp()
    {
        await ProcessVideo(ServiceType.YtShort);
    }

    public bool CheckSanity()
    {
        return configuration.Chats.Contains(Chat.Id);
    }

    public async Task ProcessVideo(ServiceType serviceType)
    {
        if (!CheckSanity())
            return;

        if (RawUpdate.Message == null)
            return;
        
        var ents = GetUrls(RawUpdate.Message);
        foreach (var url in ents.Select(ent => ent.Value))
        {
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var stream = serviceType switch
            {
                ServiceType.YtShort or ServiceType.Instagram => 
                    await ytDlpIdlService.GetVideo(url, serviceType, cts),

                ServiceType.TikTok => 
                    await tiktokIdlService.GetVideo(url, serviceType, cts),

                _ => null
            };

            if (stream == null)
            {
                if (serviceType == ServiceType.TikTok)
                {
                    stream = await ytDlpIdlService.GetVideo(url, serviceType, cts);
                }
                else
                    continue;
            }
                
            var size = ByteSize.FromBytes(stream.Length);
            logger.LogInformation("File size is {size:F}mb", size.MegaBytes);
            if (size.MegaBytes > 50)
                continue;
            await Bot.SendChatActionAsync(Chat.Id, ChatAction.UploadVideo, cancellationToken: cts.Token);
            using var ms = new MemoryStream(stream);
            await Bot.SendVideoAsync(Chat.Id, new InputFile(ms), replyToMessageId: RawUpdate.Message.MessageId, cancellationToken: cts.Token);
            return;
        }
        
    }

    private static List<KeyValuePair<MessageEntity, string>> GetUrls(Message message) =>
            Enumerable.Range(0, message.Entities.Length)
            .Select(i => new KeyValuePair<MessageEntity, string>(message.Entities.ElementAt(i), message.EntityValues.ElementAt(i)))
            .Where(ent => ent.Key.Type == MessageEntityType.Url)
            .ToList();



}