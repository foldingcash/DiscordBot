namespace DiscordBot.Core
{
    public class DiscordBotModuleProvider : IDiscordBotModuleService
    {
        public string GetFoldingAtHomeUrl()
        {
            return "return the folding@home url";
        }

        public string GetFoldingBrowserUrl()
        {
            return "return the folding browser url";
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
            return "return the web client url";
        }

        public string Help()
        {
            return "show the list of commands";
        }

        public string LookupUser()
        {
            return "allow the user to look themselves up";
        }
    }
}