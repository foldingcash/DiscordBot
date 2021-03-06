namespace DiscordBot.Console
{
    using DiscordBot.Core;
    using DiscordBot.Core.FoldingBot;
    using DiscordBot.Core.Interfaces;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

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
                services.AddHostedService<Bot>();
                services.AddSingleton<ICommandService, CommandProvider>();

                services.Configure<BotConfig>(context.Configuration.GetSection("AppSettings"));

                services.AddSingleton<IFoldingBotModuleService, FoldingBotModuleProvider>();

                services.Configure<FoldingBotConfig>(context.Configuration.GetSection("AppSettings"));
            });
        }
    }
}