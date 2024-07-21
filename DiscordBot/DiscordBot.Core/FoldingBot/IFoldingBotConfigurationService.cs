using System;

namespace DiscordBot.Core.FoldingBot
{
    public interface IFoldingBotConfigurationService : IBotConfigurationService
    {
        void ClearDistroDate();
        DateTime? GetDistroDate();
        void UpdateDistroDate(DateTime date);
    }
}