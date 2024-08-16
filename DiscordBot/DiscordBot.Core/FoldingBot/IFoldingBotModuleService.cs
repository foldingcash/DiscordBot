namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.Threading.Tasks;

    public interface IFoldingBotModuleService
    {
        Func<string, Task> Reply { set; }

        string ChangeDistroDate(DateTime date);

        string GetDistributionAnnouncement();
        string GetDonationLinks();
        string GetFoldingAtHomeUrl();

        string GetHomeUrl();

        string GetNextDistributionDate();

        Task<string> GetUserStats(string bitcoinAddress);

        Task<string> LookupUser(string searchCriteria);
    }
}