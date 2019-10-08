namespace DiscordBot.Core
{
    using System.Threading;
    using System.Threading.Tasks;

    using Discord;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;

    public class DiscordBot : IHostedService
    {
        private readonly IConfiguration configuration;

        private DiscordClient discordClient;

        public DiscordBot(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            string discordToken = GetDiscordToken();

            discordClient = new DiscordClient();
            await discordClient.Connect(discordToken, TokenType.Bot);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (discordClient is null)
                return;

            await discordClient?.Disconnect();
            discordClient?.Dispose();
            discordClient = null;
        }

        private string GetDiscordToken()
        {
            return configuration.GetSection("AppSettings")["Token"];
        }
    }
}