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

    private static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        int retries,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= retries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt == retries)
                    break;

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException!;
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

                ServiceType.TikTok => await RetryAsync(
                    () => dlService.GetVideo<YtDlpDlService>(url, serviceType, cts),
                    retries: 2,
                    delay: TimeSpan.FromSeconds(3),
                    cancellationToken: cts.Token
                ),

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
            await Bot.SendChatAction(Chat.Id, ChatAction.UploadVideo, cancellationToken: cts.Token);
            using var ms = new MemoryStream(stream);
            await Bot.SendVideo(Chat.Id, video: InputFile.FromStream(ms), 
                    replyParameters: new ReplyParameters {MessageId = RawUpdate.Message.MessageId}, 
                    cancellationToken: cts.Token
                );
            return;
        }
        
    }

    private static List<KeyValuePair<MessageEntity, string>> GetUrls(Message message) =>
            Enumerable.Range(0, message.Entities.Length)
            .Select(i => new KeyValuePair<MessageEntity, string>(message.Entities.ElementAt(i), message.EntityValues.ElementAt(i)))
            .Where(ent => ent.Key.Type == MessageEntityType.Url)
            .ToList();


}