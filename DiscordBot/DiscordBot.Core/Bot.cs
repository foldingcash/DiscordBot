namespace DiscordBot.Core
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class Bot : IHostedService, IDisposable
    {
        private readonly IBotConfigurationService botConfigurationService;

        private readonly IOptionsMonitor<BotSettings> botSettingsMonitor;

        private readonly ICommandService commandService;

        private readonly IHostEnvironment environment;

        private readonly ILogger<Bot> logger;

        private readonly IServiceProvider serviceProvider;

        private DiscordSocketClient client;

        private IBotTimerService[] timers;

        public Bot(ICommandService commandService, ILogger<Bot> logger, IHostEnvironment environment,
            IOptionsMonitor<BotSettings> botSettingsMonitor, IBotConfigurationService botConfigurationService,
            IServiceProvider serviceProvider, DiscordSocketClient client)
        {
            this.commandService = commandService;
            this.logger = logger;
            this.environment = environment;
            this.botSettingsMonitor = botSettingsMonitor;
            this.botConfigurationService = botConfigurationService;
            this.serviceProvider = serviceProvider;
            this.client = client;
        }

        private BotSettings BotSettings => botSettingsMonitor?.CurrentValue ?? new BotSettings();

        public void Dispose()
        {
            foreach (IBotTimerService t in timers)
            {
                t.Dispose();
            }

            timers = null;

            client.Dispose();
            client = null;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                LogStartup();
                await botConfigurationService.ReadConfiguration();
                await commandService.AddModulesAsync();
                await client.LoginAsync(TokenType.Bot, BotSettings.Token);
                await client.StartAsync();

                client.MessageReceived += HandleMessageReceived;

                timers = serviceProvider.GetServices<IBotTimerService>().ToArray();
                foreach (IBotTimerService t in timers)
                {
                    t.Start();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "There was an unhandled exception during startup.");
                await StopAsync(cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (IBotTimerService t in timers)
            {
                t.Stop();
                t.Close();
            }

            await client.LogoutAsync();
            await client.StopAsync();
        }

        private async Task HandleMessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message))
            {
                return;
            }

            if (message.Source != MessageSource.User)
            {
                return;
            }

            var commandContext = new SocketCommandContext(client, message);

            var argumentPosition = 0;
            if (!message.HasMentionPrefix(client.CurrentUser, ref argumentPosition))
            {
                // The message @Bot will come as <@{Id}> but client.CurrentUser.Mention is <@!{Id}>
                // So to be safe check for both in case it is changed...
                if (message.Content.Equals(client.CurrentUser.Mention)
                    || message.Content.Equals(client.CurrentUser.Mention.Replace("!", string.Empty)))
                {
                    IResult defaultResponseResult =
                        await commandService.ExecuteDefaultResponse(commandContext, argumentPosition);
                    await LogResult(commandContext, defaultResponseResult);
                }

                argumentPosition = 0;
                if (!message.HasCharPrefix('!', ref argumentPosition))
                {
                    // Not going to respond
                    return;
                }
            }

            IResult result = await commandService.ExecuteAsync(commandContext, argumentPosition);

            await LogResult(commandContext, result);
        }

        private void LogEnvironment()
        {
            logger.LogInformation("Hosting environment: {environment} PID: {PID}", environment.EnvironmentName,
                Process.GetCurrentProcess().Id);
        }

        private async Task LogResult(SocketCommandContext commandContext, IResult result)
        {
            if (result.Error.HasValue && result.Error.Value != CommandError.UnknownCommand)
            {
                await commandContext.Channel.SendMessageAsync(result.ToString());
            }
        }

        private void LogStartup()
        {
            logger.LogInformation("Bot starting");
            LogEnvironment();
        }
    }
}