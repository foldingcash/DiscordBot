namespace DiscordBot.Core.Modules
{
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;

    using DiscordBot.Interfaces;

    using Microsoft.Extensions.Logging;

    public class FoldingBotModule : ModuleBase<SocketCommandContext>
    {
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
        [Development]
        public Task GetMarketValue()
        {
            return ReplyAsync(service.GetMarketValue());
        }

        [Command("distribution")]
        [Summary("Get the date of our next distribution")]
        [Development]
        public Task GetNextDistributionDate()
        {
            return ReplyAsync(service.GetNextDistributionDate());
        }

        [Command("user")]
        [Summary("Get user stats")]
        [Development]
        public Task GetUserStats()
        {
            return ReplyAsync(service.GetUserStats());
        }

        [Command("nacl")]
        [Summary("Use the web client to start folding today")]
        public Task GetWebClientUrl()
        {
            return ReplyAsync(service.GetWebClientUrl());
        }

        [Command("help")]
        [Summary("Show the list of available commands")]
        public async Task Help()
        {
            await ReplyAsync(service.Help());
        }

        [Command("lookup")]
        [Summary("Search for a user")]
        [Development]
        public async Task LookupUser(string username)
        {
            //var hourglass = new Emoji("\u23F3");
            //await Context.Message.AddReactionAsync(hourglass);
            //await Context.Message.RemoveReactionAsync(hourglass, Context.User, RequestOptions.Default);

            await ReplyAsync(await service.LookupUser(username));
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