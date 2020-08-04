﻿namespace DiscordBot.Core.FoldingBot
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

    using DiscordBot.Core.Attributes;
    using DiscordBot.Core.Extensions;
    using DiscordBot.Core.Interfaces;
    using DiscordBot.Core.Models;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class FoldingBotModuleProvider : IDiscordBotModuleService
    {
        private readonly ICommandService commandService;

        private readonly IConfiguration configuration;

        private readonly ILogger<FoldingBotModuleProvider> logger;

        private Func<string, Task> reply = message => Task.CompletedTask;

        public FoldingBotModuleProvider(ILogger<FoldingBotModuleProvider> logger, IConfiguration configuration,
                                        ICommandService commandService)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.commandService = commandService;
        }

        public Func<string, Task> Reply
        {
            set => reply = value;
        }

        public string GetFoldingAtHomeUrl()
        {
            return $"Visit {configuration.GetAppSetting("FoldingAtHomeUrl")} to download folding@home";
        }

        public string GetHomeUrl()
        {
            return $"Visit {configuration.GetAppSetting("HomeUrl")} to learn more about this project";
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

            DistroResponse distroResponse = await CallApi<DistroResponse>(requestUri);

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

        public string Help()
        {
            IEnumerable<CommandInfo> commandList = GetCommands();

            var builder = new StringBuilder();

            builder.AppendLine(
                "Are you trying to use me? Tag me, tell me a command, and provide additional information when needed.");
            builder.AppendLine();
            builder.Append($"Usage: @{configuration.GetAppSetting("BotName")} ");
            builder.AppendLine("{command} {data}");
            builder.AppendLine();
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

            MembersResponse membersResponse = await CallApi<MembersResponse>(requestUri);

            if (membersResponse == default)
            {
                return "The api is down :( try again later";
            }

            IEnumerable<Member> matchingMembers = membersResponse.Members.Where(member =>
                member.UserName.StartsWith(searchCriteria,
                    StringComparison
                        .CurrentCultureIgnoreCase)
                || member.UserName.EndsWith(searchCriteria,
                    StringComparison
                        .CurrentCultureIgnoreCase));

            if (matchingMembers.IsNullOrEmpty())
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

        private async Task<T> CallApi<T>(Uri requestUri)
            where T : BaseResponse
        {
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

        private IEnumerable<CommandInfo> GetCommands()
        {
            List<CommandInfo> commands = commandService.GetCommands().ToList();
            commands.Sort((command1, command2) =>
                string.Compare(command1.Name, command2.Name, StringComparison.CurrentCulture));
            return commands;
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