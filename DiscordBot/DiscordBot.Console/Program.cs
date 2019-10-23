namespace DiscordBot.Console
{
    using DiscordBot.Core;
    using DiscordBot.Core.Modules;
    using DiscordBot.Interfaces;

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
            return Host.CreateDefaultBuilder(args).ConfigureServices(services =>
            {
                services.AddLogging();

                services.AddHostedService<Bot>();

                services.AddTransient<ICommandService, CommandProvider>();
                services.AddTransient<IDiscordBotModuleService, FoldingBotModuleProvider>();
            });
        }
    }
}