namespace DiscordBot.Core
{
    public interface IDiscordBotModuleService
    {
        string GetFoldingAtHomeUrl();

        string GetFoldingBrowserUrl();

        string GetMarketValue();

        string GetNextDistributionDate();

        string GetUserStats();

        string GetWebClientUrl();

        string Help();

        string LookupUser();
    }
}