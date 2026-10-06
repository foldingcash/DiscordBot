namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Models;

    public class FoldingBotModuleProvider : IFoldingBotModuleService
    {
        private const string ApiDownMessage = "The api is down :( try again later";

        private const string DisplayDateFormat = "MM/dd/yyyy";

        private readonly IFoldingApiService foldingApiService;

        private readonly IFoldingBotConfigurationService foldingBotConfigurationService;

        private readonly IOptionsMonitor<FoldingBotSettings> foldingBotSettingsMonitor;

        private readonly ILogger<FoldingBotModuleProvider> logger;

        private readonly TimeProvider timeProvider;

        public FoldingBotModuleProvider(ILogger<FoldingBotModuleProvider> logger,
            IOptionsMonitor<FoldingBotSettings> foldingBotSettingsMonitor,
            IFoldingBotConfigurationService foldingBotConfigurationService,
            IFoldingApiService foldingApiService,
            TimeProvider timeProvider)
        {
            this.logger = logger;
            this.foldingBotSettingsMonitor = foldingBotSettingsMonitor;
            this.foldingBotConfigurationService = foldingBotConfigurationService;
            this.foldingApiService = foldingApiService;
            this.timeProvider = timeProvider;
        }

        private FoldingBotSettings FoldingBotSettings =>
            foldingBotSettingsMonitor?.CurrentValue ?? new FoldingBotSettings();

        private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

        public async Task<string> ChangeDistroDate(DateTime date)
        {
            if (date.Date < UtcNow.Date)
            {
                return "The provided date is in the past, distro date was not updated.";
            }

            await foldingBotConfigurationService.UpdateDistroDate(date.Date);
            logger.LogInformation("Distro date changed to {distroDate}", date.Date);
            return $"New distro date is {FormatDate(date)}";
        }

        public async Task<string> GetDistributionAnnouncement()
        {
            DateTime distroDate = await GetDistributionDate();
            return $"Start folding now! The next distribution is {FormatDate(distroDate)}.";
        }

        public string GetDonationLinks()
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Donate BitcoinCash - {FoldingBotSettings.BitcoinCashAddress}");
            builder.AppendLine($"Donate FLDCH or another CashToken - {FoldingBotSettings.CashTokensAddress}");
            builder.AppendLine($"Visit to learn other ways to donate - {FoldingBotSettings.DonationUrl}");

            return builder.ToString();
        }

        public string GetFoldingAtHomeUrl()
        {
            return $"Visit {FoldingBotSettings.FoldingAtHomeUrl} to download folding@home";
        }

        public string GetHomeUrl()
        {
            return $"Visit {FoldingBotSettings.HomeUrl} to learn more about this project";
        }

        public async Task<string> GetNetworkStats()
        {
            (DistroResponse distroResponse, bool isLastMonth) = await GetCurrentDistro();

            if (distroResponse is null)
            {
                return ApiDownMessage;
            }

            var builder = new StringBuilder();
            AppendDistroHeader(builder, distroResponse, isLastMonth);

            if (distroResponse.DistroCount == 1)
            {
                builder.AppendLine($"There is {distroResponse.DistroCount} folder folding for FoldingCash");
            }
            else
            {
                builder.AppendLine($"There are {distroResponse.DistroCount} folders folding for FoldingCash");
            }

            builder.AppendLine($"We have folded {distroResponse.TotalPoints} points");
            builder.AppendLine($"We have folded {distroResponse.TotalWorkUnits} work units");

            return builder.ToString();
        }

        public async Task<string> GetNextDistributionDate()
        {
            DateTime distributionDate = await GetDistributionDate();

            if (distributionDate == UtcNow.Date)
            {
                return "The distribution is today!";
            }

            return $"The next distribution is {FormatDate(distributionDate)}";
        }

        public async Task<string> GetTopUsers()
        {
            static string ShortenAddress(string address)
            {
                const int length = 4;
                if (string.IsNullOrEmpty(address) || address.Length <= length * 2)
                {
                    return address;
                }

                return $"{address[..length]}...{address[^length..]}";
            }

            (DistroResponse distroResponse, bool isLastMonth) = await GetCurrentDistro();

            if (distroResponse is null)
            {
                return ApiDownMessage;
            }

            var builder = new StringBuilder();
            AppendDistroHeader(builder, distroResponse, isLastMonth);

            List<DistroUser> topUsers = (distroResponse.Distro ?? new List<DistroUser>())
                                        .OrderByDescending(user => user.PointsGained).Take(10).ToList();
            builder.AppendLine($"The top {topUsers.Count} users are:");

            foreach (DistroUser user in topUsers)
            {
                builder.AppendLine(
                    $"\t{ShortenAddress(user.CashTokensAddress)} : {user.PointsGained} points : {Math.Round(user.Amount, 2)}%");
            }

            return builder.ToString();
        }

        public async Task<string> GetUserStats(string cashTokensAddress)
        {
            (DistroResponse distroResponse, bool isLastMonth) = await GetCurrentDistro();

            if (distroResponse is null)
            {
                return ApiDownMessage;
            }

            DistroUser distroUser =
                distroResponse.Distro?.FirstOrDefault(user => user.CashTokensAddress == cashTokensAddress);

            if (distroUser is null)
            {
                return "I was unable to find your CashTokens address. Ensure your address is correct and try again.";
            }

            var builder = new StringBuilder();
            AppendDistroHeader(builder, distroResponse, isLastMonth);
            builder.AppendLine($"Results for: {distroUser.CashTokensAddress}");
            builder.AppendLine($"\tPoints gained: {distroUser.PointsGained}");
            builder.AppendLine($"\tWork units gained: {distroUser.WorkUnitsGained}");

            return builder.ToString();
        }

        public async Task<string> HealthCheck()
        {
            HealthResponse healthResponse = await foldingApiService.HealthCheck();
            return $"The bot is Healthy. The API is {healthResponse?.Status ?? "Unhealthy"}.";
        }

        public async Task<string> LookupUser(string searchCriteria)
        {
            const int maxUsers = 5;

            MembersResponse membersResponse = await foldingApiService.GetAllMembers();

            if (membersResponse?.Members is null)
            {
                return ApiDownMessage;
            }

            List<string> matchingUserNames = membersResponse.Members
                                                            .Select(member => member.UserName)
                                                            .Where(userName => userName != null
                                                                && (userName.StartsWith(searchCriteria,
                                                                        StringComparison.CurrentCultureIgnoreCase)
                                                                    || userName.EndsWith(searchCriteria,
                                                                        StringComparison.CurrentCultureIgnoreCase)))
                                                            .Distinct()
                                                            .ToList();

            if (matchingUserNames.Count == 0)
            {
                return "No matches found. Ensure you are searching the start or ending of your username and try again.";
            }

            var response = new StringBuilder();
            response.AppendLine(
                $"Showing {Math.Min(maxUsers, matchingUserNames.Count)} of {matchingUserNames.Count} matches:");
            response.AppendJoin(Environment.NewLine, matchingUserNames.Take(maxUsers));

            return response.ToString();
        }

        private static void AppendDistroHeader(StringBuilder builder, DistroResponse distroResponse, bool isLastMonth)
        {
            if (isLastMonth)
            {
                builder.AppendLine("This month's stats are not yet available...showing last month");
            }

            builder.AppendLine(
                $"Start: {FormatDate(distroResponse.Start)} End: {FormatDate(distroResponse.End)}");
        }

        private static string FormatDate(DateTime date)
        {
            return date.ToString(DisplayDateFormat, CultureInfo.InvariantCulture);
        }

        private static DateTime GetFirstSaturday(int year, int month)
        {
            var date = new DateTime(year, month, 1);
            int daysUntilSaturday = ((int) DayOfWeek.Saturday - (int) date.DayOfWeek + 7) % 7;
            return date.AddDays(daysUntilSaturday);
        }

        private async Task<(DistroResponse response, bool isLastMonth)> GetCurrentDistro()
        {
            DateTime now = UtcNow;
            var startDate = new DateTime(now.Year, now.Month, 1);
            DateTime endDate = now;
            var isLastMonth = false;

            // The API needs a couple days into the month before it has stats to report
            if (now.Day < 3)
            {
                isLastMonth = true;
                startDate = startDate.AddMonths(-1);
                endDate = new DateTime(startDate.Year, startDate.Month,
                    DateTime.DaysInMonth(startDate.Year, startDate.Month));
            }

            DistroResponse response = await foldingApiService.GetDistro(startDate, endDate, 100);
            return (response, isLastMonth);
        }

        /// <summary>
        ///     The next distribution is the admin configured date when set, otherwise the first Saturday of the month.
        /// </summary>
        private async Task<DateTime> GetDistributionDate()
        {
            DateTime today = UtcNow.Date;

            DateTime? configuredDate = foldingBotConfigurationService.GetDistroDate()?.Date;
            if (configuredDate < today)
            {
                await foldingBotConfigurationService.ClearDistroDate();
                configuredDate = null;
            }

            if (configuredDate.HasValue)
            {
                return configuredDate.Value;
            }

            DateTime distributionDate = GetFirstSaturday(today.Year, today.Month);
            if (distributionDate < today)
            {
                DateTime nextMonth = today.AddMonths(1);
                distributionDate = GetFirstSaturday(nextMonth.Year, nextMonth.Month);
            }

            return distributionDate;
        }
    }
}
