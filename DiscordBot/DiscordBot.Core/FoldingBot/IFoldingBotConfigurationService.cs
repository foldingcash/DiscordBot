using System;
using System.Threading.Tasks;

namespace DiscordBot.Core.FoldingBot
{
    public interface IFoldingBotConfigurationService : IBotConfigurationService
    {
        Task ClearDistroDate();
        DateTime? GetDistroDate();
        Task UpdateDistroDate(DateTime date);
    }
}