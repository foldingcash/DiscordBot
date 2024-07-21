using System.Collections.Generic;

namespace DiscordBot.Core
{
    public interface IBotConfigurationService
    {
        void AddDisabledCommands(string commandName);
        void ReadConfiguration();
        void RemoveDisabledCommands(string commandName);
        bool DisabledCommandsContains(string name);
    }
}