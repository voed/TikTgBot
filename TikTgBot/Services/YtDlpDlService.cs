using YoutubeDLSharp;
using System.IO;
using YoutubeDLSharp.Options;
using Microsoft.Extensions.Logging;

namespace TikTgBot.Services;

public class YtDlpDlService(Configuration configuration, ILogger<YtDlpDlService> logger)
    : IDlService
{
    private readonly YoutubeDL _ytdl = new()
    {
        OutputFolder = Path.GetTempPath(),
        //YoutubeDLPath = "yt-dlp"
    };
    private readonly ILogger _logger = logger;

    async Task<byte[]?> IDlService.GetVideo<T>(string url, ServiceType serviceType, CancellationTokenSource cts)
    {
        var format =
            "bv*[height=1280][width=720]+ba/" +
            "b[height=1280][width=720]/" +
            "bv*[height<=1280][width<=720]+ba/" +
            "b[height<=1280][width<=720]/" +
            "bv*+ba/b";
        try
        {
            var options = new OptionSet()
            {
                
                Cookies = "",
                RestrictFilenames = true,
                MaxFilesize = "45M",
                NoPart = true,
                ForceIPv4 = true,
                
                ExtractorArgs = new MultiValue<string>("youtube:player-client=web,default;po_token=web+MlvNQfHZOaB7z815fmS6HMFrRUbMb0eGNOiMuFIBkm9JyEYpifZjxcTydPCvq5xXmHdn2yJaLnlukvM0K4GTmrnH7Pm1qXmqCDPF1kn254ymREH8uTHP0qc0eYIg"),

            };
            if (serviceType != ServiceType.TikTok)
            {
                options.Format = format;
            }

            switch (serviceType)
            {
                case ServiceType.TikTok:
                    break;
                case ServiceType.Instagram:
                    options.MergeOutputFormat = DownloadMergeFormat.Mp4;

                    options.AddHeaders = new MultiValue<string>(
                        "User-Agent:Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) " +
                        "AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 " +
                        "Mobile/15E148 Safari/604.1"
                    );
                    break;
            }

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