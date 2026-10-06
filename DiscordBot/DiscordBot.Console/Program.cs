namespace DiscordBot.Console
{
    using System;
    using Core;
    using Core.FoldingBot;
    using Discord;
    using Discord.WebSocket;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    public class Program
    {
        public static void Main(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            ConfigureServices(builder.Services, builder.Configuration);
            builder.Build().Run();
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddWindowsService();

            services.AddHttpClient(ClientTypes.FoldingCashApi, (serviceProvider, client) =>
            {
                var settings = serviceProvider.GetRequiredService<IOptions<FoldingBotSettings>>();
                client.BaseAddress = new Uri(settings.Value.FoldingApiUri, UriKind.Absolute);
            });

            // MessageContent is a privileged intent, it must also be enabled in the Discord Developer Portal.
            // It is needed to read "!" prefixed commands in guild channels; mentions and DMs work without it.
            services.AddSingleton(_ => new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
                                 | GatewayIntents.GuildMessages
                                 | GatewayIntents.DirectMessages
                                 | GatewayIntents.MessageContent
            }));

            services
                .AddSingleton(TimeProvider.System)
                .AddHostedService<Bot>()
                .AddSingleton<ICommandService, CommandProvider>()
                .Configure<BotSettings>(configuration.GetSection("AppSettings"));

            services
                .Configure<FoldingBotSettings>(configuration.GetSection("AppSettings"))
                .Configure<FoldingCashApiTimerSettings>(configuration.GetSection(nameof(FoldingCashApiTimerSettings)))
                .AddSingleton<IFoldingBotConfigurationService, FoldingBotConfigurationProvider>()
                .AddSingleton<IBotConfigurationService>(provider =>
                    provider.GetRequiredService<IFoldingBotConfigurationService>())
                .AddSingleton<IFoldingApiService, FoldingApiProvider>()
                .AddSingleton<IBotTimerService, FoldingCashApiTimerProvider>()
                .AddSingleton<IFoldingBotModuleService, FoldingBotModuleProvider>();
        }
    }
}
