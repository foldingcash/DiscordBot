namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;

    using Castle.Core.Internal;

    using Discord.Commands;

    using DiscordBot.Core.Models;
    using DiscordBot.Interfaces;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class FoldingBotModuleProvider : IDiscordBotModuleService
    {
        private readonly ICommandService commandService;

        private readonly IConfiguration configuration;

        private readonly ILogger<FoldingBotModuleProvider> logger;

        public FoldingBotModuleProvider(ILogger<FoldingBotModuleProvider> logger, IConfiguration configuration,
                                        ICommandService commandService)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.commandService = commandService;
        }

        public string GetFoldingAtHomeUrl()
        {
            return $"Visit {configuration.GetAppSetting("FoldingAtHomeUrl")} to download folding@home";
        }

        public string GetFoldingBrowserUrl()
        {
            return $"Visit {configuration.GetAppSetting("FoldingBrowserUrl")} to download the folding browser";
        }

        public async Task<string> GetMarketValue()
        {
            var coinMarketCapUri = new Uri(configuration.GetAppSetting("CoinMarketCap.Uri"), UriKind.Absolute);
            var getFoldingCoinValuePath = new Uri("/v1/ticker/foldingcoin?convert=USD", UriKind.Relative);
            var requestUri = new Uri(coinMarketCapUri, getFoldingCoinValuePath);

            var serializer = new DataContractJsonSerializer(typeof (CoinMarketCapMarketValueResponse[]));

            using (var client = new HttpClient())
            {
                logger.LogInformation("Starting GET from URI: {URI}", requestUri.ToString());

                HttpResponseMessage httpResponse = await client.GetAsync(requestUri);

                logger.LogInformation("Finished GET from URI");

                string responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    logger.LogError("The response status code: {statusCode} responseContent: {responseContent}",
                        httpResponse.StatusCode, responseContent);
                    return "The api is down :( try again later";
                }

                logger.LogTrace("responseContent: {responseContent}", responseContent);

                using (var streamReader = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
                {
                    var marketValueResponses =
                        serializer.ReadObject(streamReader) as CoinMarketCapMarketValueResponse[];

                    if (marketValueResponses is null || marketValueResponses.Length == 0)
                    {
                        return "The api is down :( try again later";
                    }

                    CoinMarketCapMarketValueResponse marketValueResponse = marketValueResponses.First();

                    var stringBuilder = new StringBuilder();

                    stringBuilder.AppendLine("Source: coinmarketcap.com");
                    stringBuilder.AppendLine($"\tName: {marketValueResponse.Name}");
                    stringBuilder.AppendLine($"\tSymbol: {marketValueResponse.Symbol}");
                    stringBuilder.AppendLine($"\tPrice in $: {marketValueResponse.PriceInUsd}");
                    stringBuilder.AppendLine($"\tPrice in BTC: {marketValueResponse.PriceInBtc}");
                    stringBuilder.AppendLine($"\tLast Updated: {marketValueResponse.LastUpdatedDateTime}");

                    return stringBuilder.ToString();
                }
            }
        }

        public string GetNextDistributionDate()
        {
            DateTime now = DateTime.Now;
            DateTime distributionDate = GetDistributionDate(now.Year, now.Month);
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
            var foldingApiUri = new Uri(configuration.GetAppSetting("FoldingApiUri"), UriKind.Absolute);
            var getDistroPath = new Uri("/v1/GetDistro/Next", UriKind.Relative);
            var requestUri = new Uri(foldingApiUri, getDistroPath);

            var serializer = new DataContractJsonSerializer(typeof (DistroResponse));

            using (var client = new HttpClient())
            {
                logger.LogInformation("Starting GET from URI: {URI}", requestUri.ToString());

                HttpResponseMessage httpResponse = await client.GetAsync(requestUri);

                logger.LogInformation("Finished GET from URI");

                string responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    logger.LogError("The response status code: {statusCode} responseContent: {responseContent}",
                        httpResponse.StatusCode, responseContent);
                    return "The api is down :( try again later";
                }

                logger.LogTrace("responseContent: {responseContent}", responseContent);

                using (var streamReader = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
                {
                    var distroResponse = serializer.ReadObject(streamReader) as DistroResponse;

                    if (distroResponse is null || !distroResponse.Success)
                    {
                        return "The api is down :( try again later";
                    }

                    DistroUser distroUser =
                        distroResponse.Distro.FirstOrDefault(user => user.BitcoinAddress == bitcoinAddress);

                    if (distroUser is null)
                    {
                        return
                            "We were unable to find your bitcoin address. Ensure the address is correct and try again.";
                    }

                    var stringBuilder = new StringBuilder();

                    stringBuilder.AppendLine($"Results for: {distroUser.BitcoinAddress}");
                    stringBuilder.AppendLine($"\tPoints gained: {distroUser.PointsGained}");
                    stringBuilder.AppendLine($"\tWork units gained: {distroUser.WorkUnitsGained}");
                    stringBuilder.AppendLine($"\tReceiving amount: {distroUser.Amount}");

                    return stringBuilder.ToString();
                }
            }
        }

        public string GetWebClientUrl()
        {
            return $"Visit {configuration.GetAppSetting("WebClientUrl")} to fold using the web client";
        }

        public string Help()
        {
            IEnumerable<CommandInfo> commandList = commandService.GetCommands();
            var builder = new StringBuilder();

            builder.AppendLine("Commands -");

            foreach (CommandInfo command in commandList)
            {
                var usageAttribute =
                    command.Attributes.FirstOrDefault(attribute => attribute is UsageAttribute) as UsageAttribute;

                if (usageAttribute is default(UsageAttribute))
                {
                    builder.AppendLine($"\t{command.Name} - {command.Summary}");
                }
                else
                {
                    builder.AppendLine($"\t{command.Name} {usageAttribute.Usage} - {command.Summary}");
                }
            }

            return builder.ToString();
        }

        public async Task<string> LookupUser(string searchCriteria)
        {
            var foldingApiUri = new Uri(configuration.GetAppSetting("FoldingApiUri"), UriKind.Absolute);
            var getMemberStatsPath = new Uri("/v1/GetMembers", UriKind.Relative);
            var requestUri = new Uri(foldingApiUri, getMemberStatsPath);

            var serializer = new DataContractJsonSerializer(typeof (MembersResponse));

            using (var client = new HttpClient())
            {
                logger.LogInformation("Starting GET from URI: {URI}", requestUri.ToString());

                HttpResponseMessage httpResponse = await client.GetAsync(requestUri);

                logger.LogInformation("Finished GET from URI");

                string responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    logger.LogError("The response status code: {statusCode} responseContent: {responseContent}",
                        httpResponse.StatusCode, responseContent);
                    return "The api is down :( try again later";
                }

                logger.LogTrace("responseContent: {responseContent}", responseContent);

                using (var streamReader = new MemoryStream(Encoding.UTF8.GetBytes(responseContent)))
                {
                    var membersResponse = serializer.ReadObject(streamReader) as MembersResponse;

                    if (membersResponse is null || !membersResponse.Success)
                    {
                        return "The api is down :( try again later";
                    }

                    IEnumerable<Member> matchingMembers = membersResponse.Members.Where(member =>
                        member.UserName.StartsWith(searchCriteria,
                            StringComparison
                                .CurrentCultureIgnoreCase)
                        || member.UserName.EndsWith(
                            searchCriteria,
                            StringComparison
                                .CurrentCultureIgnoreCase));

                    if (matchingMembers.IsNullOrEmpty())
                    {
                        return
                            "No matches found. Ensure you are searching the start or ending of your username and try again.";
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
            }
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
    }
}