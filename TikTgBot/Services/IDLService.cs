namespace TikTgBot.Services;

public interface IDlService
{
    Task<byte[]?> GetVideo(string url, ServiceType serviceType, CancellationTokenSource cts);
}

public enum ServiceType
{
    TikTok,
    YtShort,
    Instagram
}