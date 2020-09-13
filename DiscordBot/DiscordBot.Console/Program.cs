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
            return Host.CreateDefaultBuilder(args).UseWindowsService().ConfigureServices(services =>
            {
                services.AddLogging();

                services.AddHostedService<Bot>();

                services.AddSingleton<ICommandService, CommandProvider>();
                services.AddSingleton<IDiscordBotModuleService, FoldingBotModuleProvider>();
            });
        }
    }
}