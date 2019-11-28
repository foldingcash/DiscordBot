namespace DiscordBot.Core.FoldingBot
{
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;

    using DiscordBot.Interfaces;
    using DiscordBot.Interfaces.Attributes;

    using Microsoft.Extensions.Logging;

    public class FoldingBotModule : ModuleBase<SocketCommandContext>
    {
        private readonly Emoji hourglass = new Emoji("\u23F3");

        private readonly ILogger<FoldingBotModule> logger;

        private readonly IDiscordBotModuleService service;

        public FoldingBotModule(IDiscordBotModuleService service, ILogger<FoldingBotModule> logger)
        {
            this.service = service;
            this.logger = logger;
        }

        [Command("bad bot")]
        [Hidden]
        public Task AcknowledgeBadBot()
        {
            return ReplyAsync("D:");
        }

        [Command("good bot")]
        [Hidden]
        public Task AcknowledgeGoodBot()
        {
            return ReplyAsync(":D");
        }

        [Command("fah")]
        [Summary("Download from Folding@Home to start folding today or update to the latest software")]
        public Task GetFoldingAtHomeUrl()
        {
            return ReplyAsync(service.GetFoldingAtHomeUrl());
        }

        [Command("browser")]
        [Summary("Download the folding browser to assist your journey into folding")]
        public Task GetFoldingBrowserUrl()
        {
            return ReplyAsync(service.GetFoldingBrowserUrl());
        }

        [Command("market")]
        [Summary("Get the current market value of our token")]
        [Deprecated]
        public async Task GetMarketValue()
        {
            await ReplyAsync(await service.GetMarketValue());
        }

        [Command("distribution")]
        [Summary("Get the date of our next distribution")]
        [Development]
        public Task GetNextDistributionDate()
        {
            return ReplyAsync(service.GetNextDistributionDate());
        }

        [Command("user")]
        [Usage("{address}")]
        [Summary("Get your stats for the next distribution based on your address")]
        [Development]
        public async Task GetUserStats(string bitcoinAddress)
        {
            await ReplyAsync(await service.GetUserStats(bitcoinAddress));
        }

        [Command("help")]
        [Summary("Show the list of available commands")]
        public async Task Help()
        {
            await ReplyAsync(service.Help());
        }

        [Command("lookup")]
        [Usage("{search criteria}")]
        [Summary("Helps to find yourself, not case sensitive and searches the start and end for a match")]
        [Development]
        public async Task LookupUser(string searchCriteria)
        {
            await ReplyAsync(await service.LookupUser(searchCriteria));
        }

        [Command("{default}")]
        [Default]
        [Hidden]
        [Summary("Show the list of available commands")]
        public async Task NoCommand()
        {
            await ReplyAsync(service.Help());
        }

        private async Task ReplyAsync(string message, [CallerMemberName] string methodName = "")
        {
            logger.LogInformation("Method Invoked: {methodName}", methodName);

            await Context.Message.AddReactionAsync(hourglass);

            await base.ReplyAsync(message);

            await Context.Message.RemoveReactionAsync(hourglass, Context.Client.CurrentUser, RequestOptions.Default);

            logger.LogInformation("Method Finished: {methodName}", methodName);
        }
    }
}