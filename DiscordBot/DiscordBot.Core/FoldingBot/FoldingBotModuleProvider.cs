﻿namespace DiscordBot.Core.FoldingBot
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

    using DiscordBot.Core.FoldingBot.Models;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class FoldingBotModuleProvider : IFoldingBotModuleService
    {
        private readonly IOptionsMonitor<FoldingBotSettings> foldingBotSettingsMonitor;
        private readonly IFoldingBotConfigurationService foldingBotConfigurationService;
        private readonly ILogger<FoldingBotModuleProvider> logger;

        private Func<string, Task> reply = message => Task.CompletedTask;

        private const string ApiDateFormat = "MM/dd/yyyy";

        public FoldingBotModuleProvider(ILogger<FoldingBotModuleProvider> logger,
                                        IOptionsMonitor<FoldingBotSettings> foldingBotSettingsMonitor, IFoldingBotConfigurationService foldingBotConfigurationService)
        {
            this.logger = logger;
            this.foldingBotSettingsMonitor = foldingBotSettingsMonitor;
            this.foldingBotConfigurationService = foldingBotConfigurationService;
        }

        private FoldingBotSettings foldingBotSettings => foldingBotSettingsMonitor?.CurrentValue ?? new FoldingBotSettings();

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
            return $"New distro date is {foldingBotConfigurationService.GetDistroDate().Value.ToShortDateString()}";
        }

        public string GetDistributionAnnouncement()
        {
            DateTime distroDate = GetDistributionDate();
            return $"Start folding now! The next distribution is {distroDate.ToShortDateString()}.";
        }

        public string GetFoldingAtHomeUrl()
        {
            return $"Visit {foldingBotSettings.FoldingAtHomeUrl} to download folding@home";
        }

        public string GetHomeUrl()
        {
            return $"Visit {foldingBotSettings.HomeUrl} to learn more about this project";
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

        public async Task<string> GetUserStats(string cashTokensAddress)
        {
            var today = DateTime.UtcNow;
            var startDate = new DateTime(today.Year, today.Month, 1);
            var endDate = today.Day == 1 ? today : today.AddDays(-1);
            var distroResponse = await CallApi<DistroResponse>($"v1/GetDistro?startDate={startDate.ToString(ApiDateFormat)}&endDate={endDate.ToString(ApiDateFormat)}&amount=10000&includeFoldingUserTypes=8");

            if (distroResponse == default)
            {
                return "The api is down :( try again later";
            }

            DistroUser distroUser = distroResponse.Distro.FirstOrDefault(user => user.CashTokensAddress == cashTokensAddress);

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
                var foldingApiUri = new Uri(foldingBotSettings.FoldingApiUri, UriKind.Absolute);
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

        public string GetDonationLinks()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Donate BitcoinCash - {foldingBotSettings.BitcoinCashAddress}");
            stringBuilder.AppendLine($"Donate FLDCH or another CashToken - {foldingBotSettings.CashTokensAddress}");
            stringBuilder.AppendLine($"Visit to learn other ways to donate - {foldingBotSettings.DonationUrl}");

            return stringBuilder.ToString();
        }

        public async Task<string> GetNetworkStats()
        {
            var today = DateTime.UtcNow;
            var startDate = new DateTime(today.Year, today.Month, 1);
            var endDate = today.Day == 1 ? today : today.AddDays(-1);
            var distroResponse = await CallApi<DistroResponse>($"v1/GetDistro?startDate={startDate.ToString(ApiDateFormat)}&endDate={endDate.ToString(ApiDateFormat)}&amount=10000&includeFoldingUserTypes=8");

            if (distroResponse == default)
            {
                return "The api is down :( try again later";
            }

            var stringBuilder = new StringBuilder();

            if (distroResponse.DistroCount == 1) 
            {
                stringBuilder.AppendLine($"There is {distroResponse.DistroCount} folder this month folding for FoldingCash");
            }
            else
            {
                stringBuilder.AppendLine($"There are {distroResponse.DistroCount} folders this month folding for FoldingCash");
            }

            stringBuilder.AppendLine($"We have folded a total of {distroResponse.TotalPoints} points");
            stringBuilder.AppendLine($"We have folded a total of {distroResponse.TotalWorkUnits} work units");

            return stringBuilder.ToString();
        }

        public async Task<string> GetTopUsers()
        {
            var today = DateTime.UtcNow;
            var startDate = new DateTime(today.Year, today.Month, 1);
            var endDate = today.Day == 1 ? today : today.AddDays(-1);
            var distroResponse = await CallApi<DistroResponse>($"v1/GetDistro?startDate={startDate.ToString(ApiDateFormat)}&endDate={endDate.ToString(ApiDateFormat)}&amount=10000&includeFoldingUserTypes=8");

            if (distroResponse == default)
            {
                return "The api is down :( try again later";
            }

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("The top 10 users are:");

            var orderedUsers = distroResponse.Distro.OrderByDescending(u => u.PointsGained).Take(10);
            foreach (var user in orderedUsers)
            {
                stringBuilder.AppendLine($"\t{user.CashTokensAddress} : {user.PointsGained}");
            }

            return stringBuilder.ToString();
        }
    }
}