namespace DiscordBot.Core.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Discord.Commands;

    using DiscordBot.Interfaces;

    using Microsoft.Extensions.Configuration;

    public class FoldingBotModuleProvider : IDiscordBotModuleService
    {
        private readonly ICommandService commandService;

        private readonly IConfiguration configuration;

        public FoldingBotModuleProvider(IConfiguration configuration, ICommandService commandService)
        {
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
                distributionDate = GetDistributionDate(now.Year + (now.Month + 1) / 13, (now.Month + 1) % 12);
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

        public string LookupUser()
        {
            return "allow the user to look themselves up";
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