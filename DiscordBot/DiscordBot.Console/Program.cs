namespace DiscordBot.Console
{
    using DiscordBot.Core;
    using DiscordBot.Core.Modules;
    using DiscordBot.Interfaces;

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
            return Host.CreateDefaultBuilder(args).ConfigureServices(services =>
            {
                services.AddLogging(options => { options.SetMinimumLevel(LogLevel.Trace); }).AddHostedService<Bot>();

                services.AddSingleton<ICommandService, CommandProvider>();
                services.AddTransient<IDiscordBotModuleService, FoldingBotModuleProvider>();
            });
        }
    }
}