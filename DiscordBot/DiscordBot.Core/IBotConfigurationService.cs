using System.Threading.Tasks;

namespace DiscordBot.Core
{
    public interface IBotConfigurationService
    {
        Task AddDisabledCommands(string commandName);
        Task ReadConfiguration();
        Task RemoveDisabledCommands(string commandName);
        bool DisabledCommandsContains(string name);
    }
}