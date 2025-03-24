namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Models;

    public class FoldingBotModuleProvider : IFoldingBotModuleService
    {
        private const string ApiDateFormat = "MM/dd/yyyy";

        private const string DisplayDateFormat = "MM/dd/yyyy";

        private readonly IFoldingBotConfigurationService foldingBotConfigurationService;

        private readonly IOptionsMonitor<FoldingBotSettings> foldingBotSettingsMonitor;

        private readonly ILogger<FoldingBotModuleProvider> logger;

        private Func<string, Task> reply = message => Task.CompletedTask;

        public FoldingBotModuleProvider(ILogger<FoldingBotModuleProvider> logger,
            IOptionsMonitor<FoldingBotSettings> foldingBotSettingsMonitor,
            IFoldingBotConfigurationService foldingBotConfigurationService)
        {
            this.logger = logger;
            this.foldingBotSettingsMonitor = foldingBotSettingsMonitor;
            this.foldingBotConfigurationService = foldingBotConfigurationService;
        }

        private FoldingBotSettings FoldingBotSettings =>
            foldingBotSettingsMonitor?.CurrentValue ?? new FoldingBotSettings();

        public Func<string, Task> Reply
        {
            set => reply = value;
        }

        public string ChangeDistroDate(DateTime date)
        {
            if (date.Date < DateTime.UtcNow.Date)
            {
                return "The provided date is in the past, distro date was not updated.";
            }

            foldingBotConfigurationService.UpdateDistroDate(date.Date);
            return $"New distro date is {foldingBotConfigurationService.GetDistroDate()?.ToShortDateString()}";
        }

        public string GetDistributionAnnouncement()
        {
            DateTime distroDate = GetDistributionDate();
            return $"Start folding now! The next distribution is {distroDate.ToShortDateString()}.";
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
            DistroResponse distroResponse = await GetCurrentDistro();

            if (distroResponse == default)
            {
                return "The api is down :( try again later";
            }

            var builder = new StringBuilder();
            AppendDistroDate(builder, distroResponse);

            if (distroResponse.DistroCount == 1)
            {
                builder.AppendLine(
                    $"There is {distroResponse.DistroCount} folder folding for FoldingCash");
            }
            else
            {
                builder.AppendLine(
                    $"There are {distroResponse.DistroCount} folders folding for FoldingCash");
            }

            builder.AppendLine($"We have folded {distroResponse.TotalPoints} points");
            builder.AppendLine($"We have folded {distroResponse.TotalWorkUnits} work units");

            return builder.ToString();
        }

        public string GetNextDistributionDate()
        {
            DateTime now = DateTime.UtcNow;
            DateTime distributionDate = GetDistributionDate();
            DateTime endDistributionDate = distributionDate.AddDays(1).AddMinutes(-1);

            if (now < distributionDate)
            {
                // do nothing
                logger.LogWarning("Now is before the distributionDate...didn't expect that!");
            }
            else if (now >= distributionDate && now <= endDistributionDate)
            {
                return "The distribution is today!";
            }
            else
            {
                DateTime nextMonth = now.AddMonths(1);
                distributionDate = GetDistributionDate(nextMonth.Year, nextMonth.Month);
            }

            return $"The next distribution is {distributionDate.ToShortDateString()}";
        }

        public async Task<string> GetTopUsers()
        {
            string ShortenAddress(string address)
            {
                const int length = 4;
                string first = address.Substring(0, length);
                string last = address.Substring(address.Length - length, length);
                return $"{first}...{last}";
            }

            decimal RoundAmount(decimal amount)
            {
                return Math.Round(amount, 2);
            }

            DistroResponse distroResponse = await GetCurrentDistro();

            if (distroResponse == default)
            {
                return "The api is down :( try again later";
            }

            var builder = new StringBuilder();
            AppendDistroDate(builder, distroResponse);

            int count = Math.Min(distroResponse.DistroCount ?? 0, 10);
            builder.AppendLine($"The top {count} users are:");

            IEnumerable<DistroUser> orderedUsers =
                distroResponse.Distro.OrderByDescending(u => u.PointsGained).Take(count);
            foreach (DistroUser user in orderedUsers)
            {
                builder.AppendLine(
                    $"\t{ShortenAddress(user.CashTokensAddress)} : {user.PointsGained} points : {RoundAmount(user.Amount)}%");
            }

            return builder.ToString();
        }

        public async Task<string> GetUserStats(string cashTokensAddress)
        {
            DistroResponse distroResponse = await GetCurrentDistro();

            if (distroResponse == default)
            {
                return "The api is down :( try again later";
            }

            DistroUser distroUser =
                distroResponse.Distro.FirstOrDefault(user => user.CashTokensAddress == cashTokensAddress);

            if (distroUser == default)
            {
                return "I was unable to find your CashTokens address. Ensure your address is correct and try again.";
            }

            var builder = new StringBuilder();
            AppendDistroDate(builder, distroResponse);
            builder.AppendLine($"Results for: {distroUser.CashTokensAddress}");
            builder.AppendLine($"\tPoints gained: {distroUser.PointsGained}");
            builder.AppendLine($"\tWork units gained: {distroUser.WorkUnitsGained}");

            return builder.ToString();
        }

        public async Task<string> LookupUser(string searchCriteria)
        {
            var membersResponse = await CallApi<MembersResponse>("v1/GetMembers/All");

            if (membersResponse == default)
            {
                return "The api is down :( try again later";
            }

            List<Member> matchingMembers = membersResponse.Members.Where(member =>
                member.UserName.StartsWith(searchCriteria, StringComparison.CurrentCultureIgnoreCase)
                || member.UserName.EndsWith(searchCriteria, StringComparison.CurrentCultureIgnoreCase)).ToList();

            if (!matchingMembers?.Any() ?? true)
            {
                return "No matches found. Ensure you are searching the start or ending of your username and try again.";
            }

            const int maxUsers = 5;
            var response = new StringBuilder();
            response.AppendLine(
                $"Showing {(matchingMembers.Count > maxUsers ? maxUsers : 5)} of {matchingMembers.Count} matches:");
            response.AppendJoin(Environment.NewLine,
                matchingMembers.Select(member => member.UserName).Distinct().Take(5));

            return response.ToString();
        }

        private void AppendDistroDate(StringBuilder builder, DistroResponse distroResponse)
        {
            builder.AppendLine(
                $"Start: {distroResponse.Start.ToString(DisplayDateFormat)} End: {distroResponse.End.ToString(DisplayDateFormat)}");
        }

        private async Task<T> CallApi<T>(string relativePath, int retryAttempts = 3, int sleepInSeconds = 300)
            where T : BaseResponse
        {
            try
            {
                var foldingApiUri = new Uri(FoldingBotSettings.FoldingApiUri, UriKind.Absolute);
                var getMemberStatsPath = new Uri(relativePath, UriKind.Relative);
                var requestUri = new Uri(foldingApiUri, getMemberStatsPath);

                var serializer = new DataContractJsonSerializer(typeof (T));

                using var client = new HttpClient();

                logger.LogInformation("Starting GET from URI: {URI}", requestUri.ToString());

                HttpResponseMessage httpResponse = await client.GetAsync(requestUri);

                logger.LogInformation("Finished GET from URI");

                string responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    logger.LogError("The response status code: {statusCode} responseContent: {responseContent}",
                        httpResponse.StatusCode, responseContent);

                    if (IsTimeout(httpResponse.StatusCode) && retryAttempts > 0)
                    {
                        await reply("The hamsters are slow today...please give us more time");
                        logger.LogDebug("Going to attempt to download again after sleeping");
                        await Task.Delay(sleepInSeconds * 1000);
                        return await CallApi<T>(relativePath, --retryAttempts);
                    }

                    return default;
                }

                logger.LogTrace("responseContent: {responseContent}", responseContent);

                await using var streamReader = new MemoryStream(Encoding.UTF8.GetBytes(responseContent));

                var response = serializer.ReadObject(streamReader) as T;

                if (response is null || !response.Success)
                {
                    return default;
                }

                return response;
            }
            catch (TaskCanceledException exception)
            {
                if (retryAttempts > 0)
                {
                    await reply("The hamsters are slow today...please give us more time");
                    logger.LogDebug(exception, "Going to attempt to download again after sleeping");
                    await Task.Delay(sleepInSeconds * 1000);
                    return await CallApi<T>(relativePath, --retryAttempts);
                }

                await reply("The api is down :( try again later");
                logger.LogError(exception, "There was an unhandled exception");
                throw;
            }
        }

        private async Task<DistroResponse>
            GetCurrentDistro()
        {
            DateTime now = DateTime.UtcNow;
            var startDate = new DateTime(now.Year, now.Month, 1);
            DateTime endDate = now;

            if (now.Day < 3)
            {
                await reply("This month's stats are not yet available...showing last month");
                startDate = startDate.AddMonths(-1);
                endDate = new DateTime(startDate.Year, startDate.Month,
                    DateTime.DaysInMonth(startDate.Year, startDate.Month));
            }

            // 8 is a magic number for bitcoin cash users
            var distroResponse = await CallApi<DistroResponse>(
                $"v1/GetDistro?startDate={startDate.ToString(ApiDateFormat)}&endDate={endDate.ToString(ApiDateFormat)}&amount=100&includeFoldingUserTypes=8");
            return distroResponse;
        }

        private DateTime GetDistributionDate()
        {
            DateTime now = DateTime.UtcNow.Date;
            DateTime defaultDistroDate = GetDistributionDate(now.Year, now.Month);
            DateTime distributionDate = foldingBotConfigurationService.GetDistroDate() ?? defaultDistroDate;
            DateTime endDistributionDate = distributionDate.AddDays(1).AddMinutes(-1);

            if (now > endDistributionDate)
            {
                DateTime nextMonth = now.AddMonths(1);
                distributionDate = GetDistributionDate(nextMonth.Year, nextMonth.Month);

                if (foldingBotConfigurationService.GetDistroDate() <= defaultDistroDate)
                {
                    foldingBotConfigurationService.ClearDistroDate();
                }
            }

            return distributionDate;
        }

        private DateTime GetDistributionDate(int year, int month)
        {
            var distributionDate = new DateTime(year, month, 1);

            while (distributionDate.DayOfWeek != DayOfWeek.Saturday)
            {
                distributionDate = distributionDate.AddDays(1);
            }

            return distributionDate;
        }

        private bool IsTimeout(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.BadGateway || statusCode == HttpStatusCode.GatewayTimeout
                                                           || statusCode == HttpStatusCode.RequestTimeout;
        }
    }
}