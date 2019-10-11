namespace DiscordBot.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    using DiscordBot.Interfaces;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Bot : IHostedService
    {
        private readonly ICommandService commandService;

        private readonly IConfiguration configuration;

        private readonly ILogger<Bot> logger;

        private DiscordSocketClient client;

        public Bot(IConfiguration configuration, ICommandService commandService, ILogger<Bot> logger)
        {
            this.configuration = configuration;
            this.commandService = commandService;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await commandService.AddModulesAsync();
                client = new DiscordSocketClient();
                string token = GetToken();
                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();

                client.MessageReceived += HandleMessageReceived;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "There was an unhandled exception during startup.");
                await StopAsync(cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await client.StopAsync();
            client.Dispose();
            client = null;
        }

        private string GetToken()
        {
            return configuration.GetSection("AppSettings")["Token"];
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

            var argumentPosition = 0;
            if (!message.HasMentionPrefix(client.CurrentUser, ref argumentPosition))
            {
                return;
            }

            var commandContext = new SocketCommandContext(client, message);
            IResult result = await commandService.ExecuteAsync(commandContext, argumentPosition);

            if (result.Error.HasValue && result.Error.Value != CommandError.UnknownCommand)
            {
                await commandContext.Channel.SendMessageAsync(result.ToString());
            }
        }
    }
}