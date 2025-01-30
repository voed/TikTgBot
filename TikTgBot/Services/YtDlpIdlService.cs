using YoutubeDLSharp;
using System.IO;
using YoutubeDLSharp.Options;
using Microsoft.Extensions.Logging;

namespace TikTgBot.Services;

public class YtDlpIdlService(Configuration configuration, ILogger<YtDlpIdlService> logger)
    : IDlService
{
    private readonly YoutubeDL _ytdl = new()
    {
        OutputFolder = Path.GetTempPath(),
        //YoutubeDLPath = "yt-dlp"
    };
    private readonly ILogger _logger = logger;

    public async Task<byte[]?> GetVideo(string url, ServiceType serviceType, CancellationTokenSource cts)
    {
        try
        {
            var options = new OptionSet()
            {
                Cookies = "",
                RestrictFilenames = true,
                MaxFilesize = "45M",
                MergeOutputFormat = DownloadMergeFormat.Unspecified,
                NoPart = true,
                ForceIPv4 = true,
                RecodeVideo = serviceType == ServiceType.Instagram ? VideoRecodeFormat.Mp4 : VideoRecodeFormat.None,
                AddHeaders = new MultiValue<string>("UserAgent:Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.79 Safari/537.36"),
                ExtractorArgs = new MultiValue<string>("youtube:player-client=web,default;po_token=web+MlvNQfHZOaB7z815fmS6HMFrRUbMb0eGNOiMuFIBkm9JyEYpifZjxcTydPCvq5xXmHdn2yJaLnlukvM0K4GTmrnH7Pm1qXmqCDPF1kn254ymREH8uTHP0qc0eYIg"),
                
                //Format = "[filesize<45M]"

            };
            options.Cookies = serviceType switch
            {
                ServiceType.Instagram => configuration.Cookies.Instagram,
                ServiceType.YtShort => configuration.Cookies.Youtube,
                ServiceType.TikTok => configuration.Cookies.TikTok,
                _ => options.Cookies
            };

            _ytdl.OutputFileTemplate = "%(id)s.%(ext)s";

            var result = await _ytdl.RunVideoDownload(url, ct: cts.Token, overrideOptions: options);


            var filename = result.Data;
            if (!File.Exists(filename))
            {
                throw new Exception(string.Join(Environment.NewLine, result.ErrorOutput));
            }

            var allBytes = await File.ReadAllBytesAsync(filename);
            File.Delete(filename);
            return allBytes;
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
        }
        return null;
    }



}