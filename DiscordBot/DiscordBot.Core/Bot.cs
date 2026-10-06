namespace DiscordBot.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class Bot : IHostedService
    {
        private readonly IBotConfigurationService botConfigurationService;

        private readonly IOptionsMonitor<BotSettings> botSettingsMonitor;

        private readonly DiscordSocketClient client;

        private readonly ICommandService commandService;

        private readonly IHostEnvironment environment;

        private readonly ILogger<Bot> logger;

        private readonly IReadOnlyList<IBotTimerService> timers;

        public Bot(ICommandService commandService, ILogger<Bot> logger, IHostEnvironment environment,
            IOptionsMonitor<BotSettings> botSettingsMonitor, IBotConfigurationService botConfigurationService,
            IEnumerable<IBotTimerService> timers, DiscordSocketClient client)
        {
            this.commandService = commandService;
            this.logger = logger;
            this.environment = environment;
            this.botSettingsMonitor = botSettingsMonitor;
            this.botConfigurationService = botConfigurationService;
            this.client = client;
            this.timers = timers.ToList();
        }

        private BotSettings BotSettings => botSettingsMonitor?.CurrentValue ?? new BotSettings();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Bot starting");
                logger.LogInformation("Hosting environment: {environment} PID: {PID}", environment.EnvironmentName,
                    Environment.ProcessId);

                await botConfigurationService.ReadConfiguration();
                await commandService.AddModulesAsync();

                client.Log += logger.LogDiscordMessage;
                client.MessageReceived += HandleMessageReceived;

                await client.LoginAsync(TokenType.Bot, BotSettings.Token);
                await client.StartAsync();

                foreach (IBotTimerService timer in timers)
                {
                    timer.Start();
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
            foreach (IBotTimerService timer in timers)
            {
                timer.Stop();
            }

            client.MessageReceived -= HandleMessageReceived;

            await client.LogoutAsync();
            await client.StopAsync();

            client.Log -= logger.LogDiscordMessage;
        }

        private async Task HandleMessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message) || message.Source != MessageSource.User)
            {
                return;
            }

            var commandContext = new SocketCommandContext(client, message);

            var argumentPosition = 0;
            if (!message.HasMentionPrefix(client.CurrentUser, ref argumentPosition))
            {
                // The message @Bot will come as <@{Id}> but client.CurrentUser.Mention may be <@!{Id}>
                // So to be safe check for both in case it is changed...
                if (message.Content.Equals(client.CurrentUser.Mention)
                    || message.Content.Equals(client.CurrentUser.Mention.Replace("!", string.Empty)))
                {
                    IResult defaultResponseResult =
                        await commandService.ExecuteDefaultResponse(commandContext, argumentPosition);
                    await ReportError(commandContext, defaultResponseResult);
                }

                argumentPosition = 0;
                if (!message.HasCharPrefix('!', ref argumentPosition))
                {
                    // Not going to respond
                    return;
                }
            }

            IResult result = await commandService.ExecuteAsync(commandContext, argumentPosition);

            await ReportError(commandContext, result);
        }

        private async Task ReportError(SocketCommandContext commandContext, IResult result)
        {
            if (result.Error.HasValue && result.Error.Value != CommandError.UnknownCommand)
            {
                logger.LogWarning("Command failed with {error}: {reason}", result.Error, result.ErrorReason);
                await commandContext.Channel.SendMessageAsync(result.ToString());
            }
        }
    }
}
