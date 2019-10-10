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

        [Command("fah")]
        public async Task GetFoldingAtHomeUrl()
        {
            await ReplyAsync(service.GetFoldingAtHomeUrl());
        }

        [Command("browser")]
        public async Task GetFoldingBrowserUrl()
        {
            await ReplyAsync(service.GetFoldingBrowserUrl());
        }

        [Command("market")]
        public async Task GetMarketValue()
        {
            await ReplyAsync(service.GetMarketValue());
        }

        [Command("distribution")]
        public async Task GetNextDistributionDate()
        {
            await ReplyAsync(service.GetNextDistributionDate());
        }

        [Command("user")]
        public async Task GetUserStats()
        {
            await ReplyAsync(service.GetUserStats());
        }

        [Command("nacl")]
        public async Task GetWebClientUrl()
        {
            await ReplyAsync(service.GetWebClientUrl());
        }

        [Command("help")]
        public async Task Help()
        {
            await ReplyAsync(service.Help());
        }

        [Command("lookup")]
        public async Task LookupUser()
        {
            await ReplyAsync(service.LookupUser());
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