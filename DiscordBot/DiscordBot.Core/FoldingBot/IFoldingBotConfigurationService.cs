namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.Threading.Tasks;

    public interface IFoldingBotConfigurationService : IBotConfigurationService
    {
        Task ClearDistroDate();

        DateTime? GetDistroDate();

        Task UpdateDistroDate(DateTime date);
    }
}