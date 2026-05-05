using BotFramework;
using BotFramework.Attributes;
using BotFramework.Enums;
using ByteSizeLib;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TikTgBot.Services;

namespace TikTgBot;

public class EventHandler(Configuration configuration, ILogger<EventHandler> logger, IDlService dlService) : BotEventHandler
{

    //language=regexp
    private const string TiktokPattern 
        = @"(https://)?(www\.)?(tiktok.com/.*?(/video/(?'video'\d{19}))|m\.tiktok\.com/v/(?'video'\d{19})\.html.*?|(?'vm'.*?(v[tm]\.tiktok\.com/)|(tiktok\.com/t/)).{9}(.*?).*?)";
    //language=regexp
    private const string YoutubePattern = @"(https://)?(www\.)?youtube\.com/shorts/(?'video'[\w-_]{11}).*?";
    //language=regexp
    private const string InstaPattern = @"instagram\.com/((?'user'.*?)/)?reel/(?'video'[\w-_]{11}).*?";


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


    public async Task ProcessVideo(ServiceType serviceType)
    {
        if (!configuration.Chats.Contains(Chat.Id))
            return;
        
        foreach (var url in GetUrls(RawUpdate.Message).Select(ent => ent.Value))
        {
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var stream = serviceType switch
            {
                ServiceType.YtShort or ServiceType.Instagram => 
                    await dlService.GetVideo<YtDlpDlService>(url, serviceType, cts),

                ServiceType.TikTok =>
                    //(await dlService.GetVideo<TiktokDlService>(url, serviceType, cts)) ??
                        await dlService.GetVideo<YtDlpDlService>(url, serviceType, cts),

                _ => null
            };

            if (stream == null)
            {
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