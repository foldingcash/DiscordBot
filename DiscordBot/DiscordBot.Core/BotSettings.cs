namespace DiscordBot.Core
{
    public class BotSettings
    {
        public string BotName { get; set; }

        public string ConfigurationPath { get; set; }

        public string Token { get; set; }

        public ulong AdminUser { get; set; }
    }
}