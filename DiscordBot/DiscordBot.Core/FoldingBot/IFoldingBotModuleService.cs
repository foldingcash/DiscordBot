namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.Threading.Tasks;

    public interface IFoldingBotModuleService
    {
        Task<string> ChangeDistroDate(DateTime date);

        Task<string> GetDistributionAnnouncement();

        string GetDonationLinks();

        string GetFoldingAtHomeUrl();

        string GetHomeUrl();

        Task<string> GetNetworkStats();

        Task<string> GetNextDistributionDate();

        Task<string> GetTopUsers();

        Task<string> GetUserStats(string cashTokensAddress);

        Task<string> HealthCheck();

        Task<string> LookupUser(string searchCriteria);
    }
}
