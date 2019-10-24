﻿namespace DiscordBot.Interfaces
{
    using System.Threading.Tasks;

    public interface IDiscordBotModuleService
    {
        string GetFoldingAtHomeUrl();

        string GetFoldingBrowserUrl();

        Task<string> GetMarketValue();

        string GetNextDistributionDate();

        string GetUserStats();

        string GetWebClientUrl();

        string Help();

        Task<string> LookupUser(string searchCriteria);
    }
}