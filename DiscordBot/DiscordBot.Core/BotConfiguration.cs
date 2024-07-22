using System.Collections;
using System.Collections.Generic;

namespace DiscordBot.Core
{
    public class BotConfiguration
    {
        public IList<string> DisabledCommands { get; set; } = new List<string>();
    }
}
