namespace DiscordBot.Core
{
    using System;
    using System.Collections.Generic;

    public static class RuntimeChanges
    {
        public static List<string> DisabledCommands = new List<string>();

        public static DateTime? DistroDateTime = null;
    }
}