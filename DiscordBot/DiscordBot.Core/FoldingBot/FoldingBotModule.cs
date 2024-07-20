namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.Threading.Tasks;

    using Discord.Commands;

    using DiscordBot.Core.Attributes;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    internal class FoldingBotModule : BotModule
    {
        private readonly IOptionsMonitor<FoldingBotConfig> configMonitor;

        private readonly ILogger logger;

        private readonly IFoldingBotModuleService service;

        public FoldingBotModule(IFoldingBotModuleService service, ILogger<FoldingBotModule> logger,
                                IOptionsMonitor<FoldingBotConfig> configMonitor)
            : base(logger)
        {
            this.service = service;
            this.logger = logger;
            this.configMonitor = configMonitor;

            service.Reply = message => Reply(message, nameof(IFoldingBotModuleService));
        }

        private FoldingBotConfig config => configMonitor.CurrentValue;

        [AdminOnly]
        [Hidden]
        [Command("announce")]
        [Summary("Announces the next distribution")]
        public async Task AnnounceUpcomingDistribution()
        {
            logger.LogDebug("Announcing the next distribution");
            await Announce(service.GetDistributionAnnouncement(), config.Guild, config.AnnounceChannel);
        }

        [AdminOnly]
        [Command("change distro")]
        [Usage("{new date}")]
        [Summary("Change the distro date to a new date")]
        public Task ChangeDistroDate(DateTime date)
        {
            return Reply(service.ChangeDistroDate(date));
        }

        [Command("fah")]
        [Summary("Start folding today or update to the latest software")]
        public Task GetFoldingAtHomeUrl()
        {
            return Reply(service.GetFoldingAtHomeUrl());
        }

        [Command("website")]
        [Summary("Learn more about this project")]
        public Task GetHomeUrl()
        {
            return Reply(service.GetHomeUrl());
        }

        [Command("distribution")]
        [Summary("Get the date of our next distribution")]
        public Task GetNextDistributionDate()
        {
            return Reply(service.GetNextDistributionDate());
        }

        [Command("user", RunMode = RunMode.Async)]
        [Usage("{address}")]
        [Summary("Get your stats for the next distribution based on your address")]
        public async Task GetUserStats(string bitcoinAddress)
        {
            await ReplyAsyncMode(async () => await service.GetUserStats(bitcoinAddress));
        }

        [Command("lookup", RunMode = RunMode.Async)]
        [Usage("{search criteria}")]
        [Summary("Helps to find yourself, not case sensitive and searches the start and end for a match")]
        public async Task LookupUser([Remainder] string searchCriteria)
        {
            await ReplyAsyncMode(async () => await service.LookupUser(searchCriteria));
        }
    }
}