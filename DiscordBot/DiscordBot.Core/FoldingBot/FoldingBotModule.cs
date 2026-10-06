namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.Threading.Tasks;
    using Attributes;
    using Discord.Commands;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    internal class FoldingBotModule : BotModule
    {
        private readonly IOptionsMonitor<FoldingBotSettings> foldingBotSettingsMonitor;

        private readonly ILogger logger;

        private readonly IFoldingBotModuleService service;

        public FoldingBotModule(ILogger<FoldingBotModule> logger,
            IOptionsMonitor<FoldingBotSettings> foldingBotSettingsMonitor,
            IOptionsMonitor<BotSettings> botSettingsMonitor,
            IFoldingBotModuleService service)
            : base(logger, botSettingsMonitor)
        {
            this.service = service;
            this.logger = logger;
            this.foldingBotSettingsMonitor = foldingBotSettingsMonitor;
        }

        private FoldingBotSettings FoldingBotSettings => foldingBotSettingsMonitor.CurrentValue;

        [AdminOnly]
        [Hidden]
        [Command("announce")]
        [Summary("Announces the next distribution")]
        public async Task AnnounceUpcomingDistribution()
        {
            logger.LogDebug("Announcing the next distribution");
            await Announce(await service.GetDistributionAnnouncement(), FoldingBotSettings.Guild,
                FoldingBotSettings.AnnounceChannel);
        }

        [AdminOnly]
        [Command("change distro")]
        [Usage("{new date}")]
        [Summary("Change the distro date to a new date")]
        public Task ChangeDistroDate(DateTime date)
        {
            return Reply(() => service.ChangeDistroDate(date));
        }

        [Command("donate")]
        [Summary("Learn how to donate to this project")]
        public Task GetDonationLinks()
        {
            return Reply(service.GetDonationLinks());
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

        [Command("network", RunMode = RunMode.Async)]
        [Summary("Show the FoldingCash network stats")]
        public Task GetNetworkStats()
        {
            return ReplyAsyncMode(service.GetNetworkStats);
        }

        [Command("distribution")]
        [Summary("Get the date of our next distribution")]
        public Task GetNextDistributionDate()
        {
            return Reply(service.GetNextDistributionDate);
        }

        [Command("top", RunMode = RunMode.Async)]
        [Summary("Show the top ten users that meet the FoldingCash requirements")]
        public Task GetTopUsers()
        {
            return ReplyAsyncMode(service.GetTopUsers);
        }

        [Command("user", RunMode = RunMode.Async)]
        [Usage("{address}")]
        [Summary("Get your stats so far this month based on your cashTokens address")]
        public Task GetUserStats(string cashTokensAddress)
        {
            return ReplyAsyncMode(() => service.GetUserStats(cashTokensAddress));
        }

        [AdminOnly]
        [Command("health", RunMode = RunMode.Async)]
        [Summary("Check if FoldingCash services are alive")]
        public Task HealthCheck()
        {
            return ReplyAsyncMode(service.HealthCheck);
        }

        [Command("lookup", RunMode = RunMode.Async)]
        [Usage("{search criteria}")]
        [Summary("Helps to find yourself, not case sensitive and searches the start and end of usernames for a match")]
        public Task LookupUser([Remainder] string searchCriteria)
        {
            return ReplyAsyncMode(() => service.LookupUser(searchCriteria));
        }

        [AdminOnly]
        [Hidden]
        [Command("verify")]
        [Usage("{btc address} {signature} {cash tokens address}")]
        [Summary("Verify yourself using your legacy Bitcoin address")]
        public Task VerifyUser(string bitcoinAddress, string signature, string cashTokensAddress)
        {
            return Task.CompletedTask;
        }
    }
}
