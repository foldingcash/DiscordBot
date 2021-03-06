namespace DiscordBot.Core.Interfaces
{
    using System;
    using System.Threading.Tasks;

    public interface IDiscordBotModuleService
    {
        Func<string, Task> Reply { set; }

        string ChangeDistroDate(DateTime date);

        string GetDistributionAnnouncement();

        string GetFoldingAtHomeUrl();

        string GetHomeUrl();

        string GetNextDistributionDate();

        Task<string> GetUserStats(string bitcoinAddress);

        string Help();

        Task<string> LookupUser(string searchCriteria);
    }
}