namespace DiscordBot.Core
{
    using System.Threading.Tasks;

    public interface IBotConfigurationService
    {
        Task AddDisabledCommands(string commandName);

        bool DisabledCommandsContains(string name);

        Task ReadConfiguration();

        Task RemoveDisabledCommands(string commandName);
    }
}