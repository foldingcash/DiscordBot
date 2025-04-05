namespace DiscordBot.Console
{
    using System;
    using Core;
    using Core.FoldingBot;
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
                services.AddHttpClient(ClientTypes.FoldingCashApi, (serviceProvider, client) =>
                {
                    var settings = serviceProvider.GetRequiredService<FoldingBotSettings>();
                    var foldingApiUri = new Uri(settings.FoldingApiUri, UriKind.Absolute);
                    client.BaseAddress = foldingApiUri;
                });

                services
                    .AddHostedService<Bot>()
                    .AddSingleton<ICommandService, CommandProvider>()
                    .Configure<BotSettings>(context.Configuration.GetSection("AppSettings"));

                services
                    .AddSingleton<IFoldingBotModuleService, FoldingBotModuleProvider>()
                    .Configure<FoldingBotSettings>(context.Configuration.GetSection("AppSettings"))
                    .AddSingleton<IFoldingBotConfigurationService, FoldingBotConfigurationProvider>()
                    .AddSingleton<IBotConfigurationService>(provider =>
                        provider.GetRequiredService<IFoldingBotConfigurationService>());
            });
        }
    }
}