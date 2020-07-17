namespace DiscordBot.Core.Interfaces
{
    using System.Threading.Tasks;

    public interface IDiscordBotModuleService
    {
        string GetFoldingAtHomeUrl();

        string GetNextDistributionDate();

        Task<string> GetUserStats(string bitcoinAddress);

        string Help();

        Task<string> LookupUser(string searchCriteria);
    }
}