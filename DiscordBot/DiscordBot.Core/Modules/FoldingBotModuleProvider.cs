namespace DiscordBot.Core.Modules
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;

    using Discord.Commands;

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

        public string GetMarketValue()
        {
            return "show the current market value of the coin";
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

        public string GetUserStats()
        {
            return "show the user their stats";
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
                builder.AppendLine($"  {command.Name} - {command.Summary}");
            }

            return builder.ToString();
        }

        public async Task<string> LookupUser(string username)
        {
            var foldingApiUri = new Uri(configuration.GetAppSetting("FoldingApiUri"), UriKind.Absolute);
            var getMemberStatsPath = new Uri("/v1/GetMembers", UriKind.Relative);
            var requestUri = new Uri(foldingApiUri, getMemberStatsPath);

            var serializer = new DataContractJsonSerializer(typeof (MemberResponse));

            using (var client = new HttpClient())
            {
                logger.LogInformation("Starting GET from URI: {URI}", requestUri.ToString());

                HttpResponseMessage httpResponse = await client.GetAsync(requestUri);

                logger.LogInformation("Finished GET from URI");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    return "The api is down :( try again later";
                }

                Stream streamReader = await httpResponse.Content.ReadAsStreamAsync();

                var response = serializer.ReadObject(streamReader) as MemberResponse;

                if (response is null || !response.Success)
                {
                    return "The api is down :( try again later";
                }

                IEnumerable<Member> matchingMembers =
                    response.Members.Where(member => member.UserName.StartsWith(username));

                return $"found the following matches: {string.Join(", ", matchingMembers)}";
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

        [DataContract]
        private class ApiError
        {
            [DataMember]
            public string ErrorCode { get; set; }

            [DataMember]
            public string ErrorMessage { get; set; }
        }

        [DataContract]
        private class Member
        {
            [DataMember]
            public string BitcoinAddress { get; set; }

            [DataMember]
            public string FriendlyName { get; set; }

            [DataMember]
            public long TeamNumber { get; set; }

            [DataMember]
            public string UserName { get; set; }
        }

        [DataContract]
        private class MemberResponse
        {
            [DataMember]
            public int? ErrorCount { get; set; }

            [DataMember]
            public IList<ApiError> Errors { get; set; }

            [DataMember]
            public string FirstErrorCode { get; set; }

            [DataMember]
            public int? MemberCount { get; set; }

            [DataMember]
            public IList<Member> Members { get; set; }

            [DataMember]
            public bool Success { get; set; }
        }
    }
}