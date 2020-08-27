namespace DiscordBot.Core.Extensions
{
    using Microsoft.Extensions.Configuration;

    public static class ConfigurationExtensions
    {
        public static string GetAppSetting(this IConfiguration configuration, string key)
        {
            return configuration.GetSection("AppSettings")[key];
        }
    }
}