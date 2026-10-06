namespace DiscordBot.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.FoldingBot;
    using Core.FoldingBot.Models;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Time.Testing;

    public class FoldingBotModuleProviderTests
    {
        private readonly FakeFoldingApiService apiService = new FakeFoldingApiService();

        private readonly FakeFoldingBotConfigurationService configurationService =
            new FakeFoldingBotConfigurationService();

        private readonly FakeTimeProvider timeProvider = new FakeTimeProvider();

        private FoldingBotModuleProvider CreateProvider(DateTime utcNow)
        {
            timeProvider.SetUtcNow(new DateTimeOffset(utcNow, TimeSpan.Zero));
            return new FoldingBotModuleProvider(NullLogger<FoldingBotModuleProvider>.Instance,
                new FakeOptionsMonitor<FoldingBotSettings>(new FoldingBotSettings()), configurationService,
                apiService, timeProvider);
        }

        // October 2026 starts on a Thursday so the first Saturday is the 3rd; November's is the 7th
        [Theory]
        [InlineData(1, "The next distribution is 10/03/2026")]
        [InlineData(2, "The next distribution is 10/03/2026")]
        [InlineData(3, "The distribution is today!")]
        [InlineData(4, "The next distribution is 11/07/2026")]
        [InlineData(31, "The next distribution is 11/07/2026")]
        public async Task GetNextDistributionDate_DefaultsToFirstSaturday(int day, string expected)
        {
            FoldingBotModuleProvider provider = CreateProvider(new DateTime(2026, 10, day, 15, 30, 0));

            Assert.Equal(expected, await provider.GetNextDistributionDate());
        }

        [Fact]
        public async Task GetNextDistributionDate_UsesConfiguredDate()
        {
            configurationService.DistroDate = new DateTime(2026, 10, 20);
            FoldingBotModuleProvider provider = CreateProvider(new DateTime(2026, 10, 4));

            Assert.Equal("The next distribution is 10/20/2026", await provider.GetNextDistributionDate());
        }

        [Fact]
        public async Task GetNextDistributionDate_ClearsPastConfiguredDate()
        {
            configurationService.DistroDate = new DateTime(2026, 9, 30);
            FoldingBotModuleProvider provider = CreateProvider(new DateTime(2026, 10, 1));

            Assert.Equal("The next distribution is 10/03/2026", await provider.GetNextDistributionDate());
            Assert.Null(configurationService.DistroDate);
        }

        [Fact]
        public async Task ChangeDistroDate_RejectsPastDates()
        {
            FoldingBotModuleProvider provider = CreateProvider(new DateTime(2026, 10, 6));

            string response = await provider.ChangeDistroDate(new DateTime(2026, 10, 5));

            Assert.Equal("The provided date is in the past, distro date was not updated.", response);
            Assert.Null(configurationService.DistroDate);
        }

        [Fact]
        public async Task ChangeDistroDate_SavesTheDate()
        {
            FoldingBotModuleProvider provider = CreateProvider(new DateTime(2026, 10, 6));

            string response = await provider.ChangeDistroDate(new DateTime(2026, 10, 24, 13, 0, 0));

            Assert.Equal("New distro date is 10/24/2026", response);
            Assert.Equal(new DateTime(2026, 10, 24), configurationService.DistroDate);
        }

        [Fact]
        public async Task GetNetworkStats_EarlyInTheMonth_ShowsLastMonth()
        {
            FoldingBotModuleProvider provider = CreateProvider(new DateTime(2026, 10, 2));

            string response = await provider.GetNetworkStats();

            Assert.StartsWith("This month's stats are not yet available...showing last month", response);
            Assert.Equal((new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)), apiService.LastDistroRequest);
        }

        [Fact]
        public async Task GetNetworkStats_LaterInTheMonth_ShowsThisMonth()
        {
            apiService.Distro.DistroCount = 1;
            FoldingBotModuleProvider provider = CreateProvider(new DateTime(2026, 10, 5));

            string response = await provider.GetNetworkStats();

            Assert.StartsWith("Start: 10/01/2026 End: 10/05/2026", response);
            Assert.Contains("There is 1 folder folding for FoldingCash", response);
            Assert.Equal((new DateTime(2026, 10, 1), new DateTime(2026, 10, 5)), apiService.LastDistroRequest);
        }

        [Fact]
        public async Task GetNetworkStats_ApiDown()
        {
            apiService.Distro = null;
            FoldingBotModuleProvider provider = CreateProvider(new DateTime(2026, 10, 5));

            Assert.Equal("The api is down :( try again later", await provider.GetNetworkStats());
        }

        [Fact]
        public async Task GetTopUsers_OrdersByPointsAndHandlesShortAddresses()
        {
            apiService.Distro.Distro = new List<DistroUser>
            {
                new DistroUser { CashTokensAddress = "short", PointsGained = 5, Amount = 10.555m },
                new DistroUser { CashTokensAddress = "bitcoincash:zrduh6df57", PointsGained = 50, Amount = 89.445m },
                new DistroUser { CashTokensAddress = null, PointsGained = 1 }
            };
            FoldingBotModuleProvider provider = CreateProvider(new DateTime(2026, 10, 5));

            string[] lines = (await provider.GetTopUsers()).Split(Environment.NewLine);

            Assert.Equal("The top 3 users are:", lines[1]);
            Assert.Equal("\tbitc...df57 : 50 points : 89.44%", lines[2]);
            Assert.Equal("\tshort : 5 points : 10.56%", lines[3]);
        }

        [Fact]
        public async Task GetUserStats_UnknownAddress()
        {
            FoldingBotModuleProvider provider = CreateProvider(new DateTime(2026, 10, 5));

            string response = await provider.GetUserStats("unknown");

            Assert.StartsWith("I was unable to find your CashTokens address", response);
        }

        [Fact]
        public async Task LookupUser_ShowsDistinctMatchesUpToFive()
        {
            apiService.Members = new MembersResponse
            {
                Members = new[] { "fold_a", "fold_b", "fold_b", "FOLD_c", "fold_d", "fold_e", "x_fold", "other" }
                          .Select(name => new Member { UserName = name }).Append(new Member()).ToList()
            };
            FoldingBotModuleProvider provider = CreateProvider(new DateTime(2026, 10, 5));

            string[] lines = (await provider.LookupUser("fold")).Split(Environment.NewLine);

            Assert.Equal("Showing 5 of 6 matches:", lines[0]);
            Assert.Equal(new[] { "fold_a", "fold_b", "FOLD_c", "fold_d", "fold_e" }, lines.Skip(1));
        }

        [Fact]
        public async Task LookupUser_NoMatches()
        {
            apiService.Members = new MembersResponse { Members = new List<Member>() };
            FoldingBotModuleProvider provider = CreateProvider(new DateTime(2026, 10, 5));

            Assert.StartsWith("No matches found.", await provider.LookupUser("fold"));
        }

        [Fact]
        public async Task HealthCheck_ReportsApiStatus()
        {
            FoldingBotModuleProvider provider = CreateProvider(new DateTime(2026, 10, 5));

            Assert.Equal("The bot is Healthy. The API is Unhealthy.", await provider.HealthCheck());

            apiService.Health = new HealthResponse { Status = "Healthy" };
            Assert.Equal("The bot is Healthy. The API is Healthy.", await provider.HealthCheck());
        }
    }
}
