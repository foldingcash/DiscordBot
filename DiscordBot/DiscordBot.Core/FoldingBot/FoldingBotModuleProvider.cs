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
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Donate BitcoinCash - {FoldingBotSettings.BitcoinCashAddress}");
            stringBuilder.AppendLine($"Donate FLDCH or another CashToken - {FoldingBotSettings.CashTokensAddress}");
            stringBuilder.AppendLine($"Visit to learn other ways to donate - {FoldingBotSettings.DonationUrl}");

            return stringBuilder.ToString();
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
            DistroResponse distroResponse = await GetCurrentMonthDistro();

            if (distroResponse == default)
            {
                return "The api is down :( try again later";
            }

            var stringBuilder = new StringBuilder();

            if (distroResponse.DistroCount == 1)
            {
                stringBuilder.AppendLine(
                    $"There is {distroResponse.DistroCount} folder this month folding for FoldingCash");
            }
            else
            {
                stringBuilder.AppendLine(
                    $"There are {distroResponse.DistroCount} folders this month folding for FoldingCash");
            }

            stringBuilder.AppendLine($"We have folded a total of {distroResponse.TotalPoints} points");
            stringBuilder.AppendLine($"We have folded a total of {distroResponse.TotalWorkUnits} work units");

            return stringBuilder.ToString();
        }

        public string GetNextDistributionDate()
        {
            DateTime now = DateTime.UtcNow;
            DateTime distributionDate = GetDistributionDate();
            DateTime endDistributionDate = distributionDate.AddDays(1).AddMinutes(-1);

            if (now < distributionDate)
            {
                // do nothing
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

            DistroResponse distroResponse = await GetCurrentMonthDistro();

            if (distroResponse == default)
            {
                return "The api is down :( try again later";
            }

            var stringBuilder = new StringBuilder();

            int count = Math.Min(distroResponse.DistroCount ?? 0, 10);
            stringBuilder.AppendLine($"The top {count} users this month are:");

            IEnumerable<DistroUser> orderedUsers =
                distroResponse.Distro.OrderByDescending(u => u.PointsGained).Take(count);
            foreach (DistroUser user in orderedUsers)
            {
                stringBuilder.AppendLine(
                    $"\t{ShortenAddress(user.CashTokensAddress)} : {user.PointsGained} points : {user.Amount}%");
            }

            return stringBuilder.ToString();
        }

        public async Task<string> GetUserStats(string cashTokensAddress)
        {
            DistroResponse distroResponse = await GetCurrentMonthDistro();

            if (distroResponse == default)
            {
                return "The api is down :( try again later";
            }

            DistroUser distroUser =
                distroResponse.Distro.FirstOrDefault(user => user.CashTokensAddress == cashTokensAddress);

            if (distroUser == default)
            {
                return "We were unable to find your bitcoin address. Ensure the address is correct and try again.";
            }

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Results for: {distroUser.CashTokensAddress}");
            stringBuilder.AppendLine($"\tPoints gained: {distroUser.PointsGained}");
            stringBuilder.AppendLine($"\tWork units gained: {distroUser.WorkUnitsGained}");

            return stringBuilder.ToString();
        }

        public async Task<string> LookupUser(string searchCriteria)
        {
            var membersResponse = await CallApi<MembersResponse>("v1/GetMembers/All");

            if (membersResponse == default)
            {
                return "The api is down :( try again later";
            }

            IEnumerable<Member> matchingMembers = membersResponse.Members.Where(member =>
                member.UserName.StartsWith(searchCriteria, StringComparison.CurrentCultureIgnoreCase)
                || member.UserName.EndsWith(searchCriteria, StringComparison.CurrentCultureIgnoreCase));

            if (!matchingMembers?.Any() ?? true)
            {
                return "No matches found. Ensure you are searching the start or ending of your username and try again.";
            }

            string response = $"Found the following matches:{Environment.NewLine}"
                              + $"\t{string.Join($",{Environment.NewLine}\t", matchingMembers.Select(member => member.UserName).Distinct())}";

            if (response.Length > 2000)
            {
                response = response.Substring(0, 2000 - 3);
                response += "...";
            }

            return response;
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

        private async Task<DistroResponse> GetCurrentMonthDistro()
        {
            DateTime today = DateTime.UtcNow;
            var startDate = new DateTime(today.Year, today.Month, 1);
            DateTime endDate = today;
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