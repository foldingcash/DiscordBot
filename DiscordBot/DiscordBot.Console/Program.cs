namespace DiscordBot.Console
{
    using DiscordBot.Core;

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
                services.AddLogging().AddHostedService<DiscordBot>();

                services.AddTransient<ICommandService, CommandProvider>();
                services.AddTransient<IDiscordBotModuleService, DiscordBotModuleProvider>();
            });
        }
    }
}