namespace DiscordBot.Core
{
    using System.Linq;
    using Discord.Commands;

    internal static class CommandInfoExtensions
    {
        public static bool HasAttribute<T>(this CommandInfo command)
        {
            return command.Attributes.OfType<T>().Any();
        }

        public static T GetAttribute<T>(this CommandInfo command)
        {
            return command.Attributes.OfType<T>().FirstOrDefault();
        }

        public static bool IsDisabled(this CommandInfo command, IBotConfigurationService botConfigurationService)
        {
            return command.Aliases.Any(botConfigurationService.DisabledCommandsContains);
        }
    }
}
