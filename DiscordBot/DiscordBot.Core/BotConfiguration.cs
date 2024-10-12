namespace DiscordBot.Core
{
    using System.Collections.Generic;

    public class BotConfiguration
    {
        public IList<string> DisabledCommands { get; set; } = new List<string>();
    }
}