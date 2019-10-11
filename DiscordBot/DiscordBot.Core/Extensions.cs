namespace DiscordBot.Core
{
    using Microsoft.Extensions.Configuration;

    internal static class Extensions
    {
        internal static string GetAppSetting(this IConfiguration configuration, string key)
        {
            return configuration.GetSection("AppSettings")[key];
        }
    }
}