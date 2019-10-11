namespace DiscordBot.Core.Modules
{
    using System.Collections.Generic;
    using System.Text;

    using Discord.Commands;

    using Microsoft.Extensions.Configuration;

    public class DiscordBotModuleProvider : IDiscordBotModuleService
    {
        private readonly ICommandService commandService;

        private readonly IConfiguration configuration;

        public DiscordBotModuleProvider(IConfiguration configuration, ICommandService commandService)
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
            return "return the next distribution date";
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
    }
}