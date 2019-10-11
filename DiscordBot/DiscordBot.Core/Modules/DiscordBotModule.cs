namespace DiscordBot.Core.Modules
{
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;

    using Microsoft.Extensions.Logging;

    public class DiscordBotModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<DiscordBotModule> logger;

        private readonly IDiscordBotModuleService service;

        public DiscordBotModule(IDiscordBotModuleService service, ILogger<DiscordBotModule> logger)
        {
            this.service = service;
            this.logger = logger;
        }

        [Command("good bot")]
        public Task AcknowledgeGoodBot()
        {
            return ReplyAsync(":D");
        }

        [Command("fah")]
        [Alias("Download from Folding@Home to start folding today or update to the latest software")]
        public Task GetFoldingAtHomeUrl()
        {
            return ReplyAsync(service.GetFoldingAtHomeUrl());
        }

        [Command("browser")]
        [Alias("Download the folding browser to assist your journey into folding")]
        public Task GetFoldingBrowserUrl()
        {
            return ReplyAsync(service.GetFoldingBrowserUrl());
        }

        [Command("market")]
        [Alias("Get the current market value of our token")]
        public Task GetMarketValue()
        {
            return ReplyAsync(service.GetMarketValue());
        }

        [Command("distribution")]
        [Alias("Get the date of our next distribution")]
        public Task GetNextDistributionDate()
        {
            return ReplyAsync(service.GetNextDistributionDate());
        }

        [Command("user")]
        [Alias("Get user stats")]
        public Task GetUserStats()
        {
            return ReplyAsync(service.GetUserStats());
        }

        [Command("nacl")]
        [Alias("Use the web client to start folding today")]
        public Task GetWebClientUrl()
        {
            return ReplyAsync(service.GetWebClientUrl());
        }

        [Command("help")]
        [Alias("Show the list of available commands")]
        public async Task Help()
        {
            await ReplyAsync(service.Help());
        }

        [Command("lookup")]
        [Alias("Search for a user")]
        public Task LookupUser()
        {
            return ReplyAsync(service.LookupUser());
        }

        private Task ReplyAsync(string message, [CallerMemberName] string methodName = "")
        {
            logger.LogInformation("Method Invoked: {methodName}", methodName);
            Task<IUserMessage> task = base.ReplyAsync(message);
            logger.LogInformation("Method Finished: {methodName}", methodName);
            return task;
        }
    }
}