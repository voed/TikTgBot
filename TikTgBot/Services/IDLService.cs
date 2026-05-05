namespace TikTgBot.Services;

public interface IDlService
{
    public Task<byte[]?> GetVideo<T>(string url, ServiceType serviceType, CancellationTokenSource cts)
        where T : class, IDlService;
}

public enum ServiceType
{
    TikTok,
    YtShort,
    Instagram
}