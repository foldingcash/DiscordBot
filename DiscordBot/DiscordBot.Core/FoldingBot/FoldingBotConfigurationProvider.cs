using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Core.FoldingBot
{
    public class FoldingBotConfigurationProvider : BotConfigurationProvider<FoldingBotConfiguration>, IFoldingBotConfigurationService
    {
        public FoldingBotConfigurationProvider(IOptionsMonitor<BotSettings> botSettingsMonitor) : base(botSettingsMonitor) { }

        public Task ClearDistroDate()
        {
            configuration.DistroDate = null;
            return WriteConfiguration();
        }

        public DateTime? GetDistroDate()
        {
            return configuration.DistroDate;
        }

        public Task UpdateDistroDate(DateTime date)
        {
            configuration.DistroDate = date;
            return WriteConfiguration();
        }
    }    
}
