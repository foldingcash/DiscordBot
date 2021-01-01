namespace DiscordBot.Console
{
    using System.Runtime.InteropServices;

    using DiscordBot.Core;
    using DiscordBot.Core.FoldingBot;
    using DiscordBot.Core.Interfaces;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args).UseWindowsService().ConfigureServices((context, services) =>
            {
                services.AddLogging(builder =>
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        builder.AddEventLog(settings =>
                        {
                            settings.SourceName = context.Configuration["Logging:EventLog:SourceName"];
                        });
                    }
                });

                services.AddHostedService<Bot>();
                services.AddSingleton<ICommandService, CommandProvider>();

                services.Configure<BotConfig>(context.Configuration.GetSection("AppSettings"));

                services.AddSingleton<IDiscordBotModuleService, FoldingBotModuleProvider>();

                services.Configure<FoldingBotConfig>(context.Configuration.GetSection("AppSettings"));
            });
        }
    }
}