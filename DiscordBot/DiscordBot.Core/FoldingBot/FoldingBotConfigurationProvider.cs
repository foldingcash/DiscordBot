using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Core.FoldingBot
{
    internal class FoldingBotConfigurationProvider : BotConfigurationProvider, IFoldingBotConfigurationService
    {
        private static DateTime? distroDate = null;

        public void ClearDistroDate()
        {
            distroDate = null;
        }

        public DateTime? GetDistroDate()
        {
            return distroDate;
        }

        public void UpdateDistroDate(DateTime date)
        {
            distroDate = date;
        }
    }
}
