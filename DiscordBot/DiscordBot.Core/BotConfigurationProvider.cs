using System.Collections.Generic;

namespace DiscordBot.Core
{
    internal class BotConfigurationProvider : IBotConfigurationService
    {
        private static readonly List<string> disabledCommands = new List<string>();

        public void AddDisabledCommands(string commandName)
        {
            disabledCommands.Add(commandName);
        }

        public bool DisabledCommandsContains(string name)
        {
            return disabledCommands.Contains(name);
        }

        public void ReadConfiguration()
        {
        }

        public void RemoveDisabledCommands(string commandName)
        {
            disabledCommands.Remove(commandName);
        }
    }
}
