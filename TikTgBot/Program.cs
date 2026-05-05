using BotFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TikTgBot.Services;

namespace TikTgBot;

class Program
{
    static void Main(string[] args)
    {
        
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                Console.WriteLine(hostContext.HostingEnvironment.EnvironmentName);
                var config = hostContext.Configuration.GetSection("Configuration").Get<Configuration>();
                if (config == null)
                {
                    throw new Exception("Could not read config file");
                }

                services.AddSingleton(config);
                services.AddTransient<YtDlpDlService>();
                services.AddTransient<TiktokDlService>();
                services.AddSingleton<IDlService, DlService>(); // Single entry point

                services.AddLogging(builder => builder.AddConsole());
                services.AddTelegramBot();
            });
}
