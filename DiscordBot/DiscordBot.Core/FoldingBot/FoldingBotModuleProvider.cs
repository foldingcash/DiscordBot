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
    using System.Threading;
    using System.Threading.Tasks;

    using DiscordBot.Core.FoldingBot.Models;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class FoldingBotModuleProvider : IFoldingBotModuleService
    {
        private readonly IOptionsMonitor<FoldingBotConfig> foldingBotConfigMonitor;

        private readonly ILogger<FoldingBotModuleProvider> logger;

        private Func<string, Task> reply = message => Task.CompletedTask;

        public FoldingBotModuleProvider(ILogger<FoldingBotModuleProvider> logger,
                                        IOptionsMonitor<FoldingBotConfig> foldingBotConfigMonitor)
        {
            this.logger = logger;
            this.foldingBotConfigMonitor = foldingBotConfigMonitor;
        }

        private FoldingBotConfig foldingBotConfig => foldingBotConfigMonitor?.CurrentValue ?? new FoldingBotConfig();

        public Func<string, Task> Reply
        {
            set => reply = value;
        }

        public string ChangeDistroDate(DateTime date)
        {
            if (date.Date < DateTime.Now.Date)
            {
                return "The provided date is in the past, distro date was not updated.";
            }

            FoldingBotRuntimeChanges.DistroDateTime = date.Date;
            return $"New distro date is {FoldingBotRuntimeChanges.DistroDateTime.Value.ToShortDateString()}";
        }

        public string GetDistributionAnnouncement()
        {
            DateTime distroDate = GetDistributionDate();
            return $"Start folding now! The next distribution is {distroDate.ToShortDateString()}.";
        }

        public string GetFoldingAtHomeUrl()
        {
            return $"Visit {foldingBotConfig.FoldingAtHomeUrl} to download folding@home";
        }

        public string GetHomeUrl()
        {
            return $"Visit {foldingBotConfig.HomeUrl} to learn more about this project";
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

        public async Task<string> GetUserStats(string bitcoinAddress)
        {
            var distroResponse = await CallApi<DistroResponse>("/v1/GetDistro/All");

            if (distroResponse == default)
            {
                return "The api is down :( try again later";
            }

            DistroUser distroUser = distroResponse.Distro.FirstOrDefault(user => user.BitcoinAddress == bitcoinAddress);

            if (distroUser is null)
            {
                return "We were unable to find your bitcoin address. Ensure the address is correct and try again.";
            }

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Results for: {distroUser.BitcoinAddress}");
            stringBuilder.AppendLine($"\tPoints gained: {distroUser.PointsGained}");
            stringBuilder.AppendLine($"\tWork units gained: {distroUser.WorkUnitsGained}");
            stringBuilder.AppendLine($"\tReceiving amount: {distroUser.Amount}");

            return stringBuilder.ToString();
        }

        public async Task<string> LookupUser(string searchCriteria)
        {
            var membersResponse = await CallApi<MembersResponse>("/v1/GetMembers");

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
                var foldingApiUri = new Uri(foldingBotConfig.FoldingApiUri, UriKind.Absolute);
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
                        Thread.Sleep(sleepInSeconds * 1000);
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
                    Thread.Sleep(sleepInSeconds * 1000);
                    return await CallApi<T>(relativePath, --retryAttempts);
                }

                await reply("The api is down :( try again later");
                logger.LogError(exception, "There was an unhandled exception");
                throw;
            }
        }

        private DateTime GetDistributionDate()
        {
            DateTime now = DateTime.UtcNow;
            DateTime defaultDistroDate = GetDistributionDate(now.Year, now.Month);
            DateTime distributionDate = FoldingBotRuntimeChanges.DistroDateTime ?? defaultDistroDate;
            DateTime endDistributionDate = distributionDate.AddDays(1).AddMinutes(-1);

            if (now > endDistributionDate)
            {
                DateTime nextMonth = now.AddMonths(1);
                distributionDate = GetDistributionDate(nextMonth.Year, nextMonth.Month);

                if (FoldingBotRuntimeChanges.DistroDateTime <= defaultDistroDate)
                {
                    FoldingBotRuntimeChanges.DistroDateTime = null;
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