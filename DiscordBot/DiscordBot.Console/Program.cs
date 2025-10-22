namespace DiscordBot.Console
{
    using System;
    using Core;
    using Core.FoldingBot;
    using Discord.WebSocket;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

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
                    var settings = serviceProvider.GetRequiredService<IOptions<FoldingBotSettings>>();
                    var foldingApiUri = new Uri(settings.Value.FoldingApiUri, UriKind.Absolute);
                    client.BaseAddress = foldingApiUri;
                });

                services.AddSingleton(_ => new DiscordSocketClient(new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true
                }));

                services
                    .AddHostedService<Bot>()
                    .AddSingleton<ICommandService, CommandProvider>()
                    .Configure<BotSettings>(context.Configuration.GetSection("AppSettings"));

                services
                    .Configure<FoldingBotSettings>(context.Configuration.GetSection("AppSettings"))
                    .AddSingleton<IFoldingBotConfigurationService, FoldingBotConfigurationProvider>()
                    .AddSingleton<IBotConfigurationService>(provider =>
                        provider.GetRequiredService<IFoldingBotConfigurationService>())
                    .AddSingleton<IFoldingApiService, FoldingApiProvider>()
                    .AddSingleton<IBotTimerService, FoldingCashApiTimerProvider>()
                    .AddSingleton<IFoldingBotModuleService, FoldingBotModuleProvider>();
            });
        }
    }
}