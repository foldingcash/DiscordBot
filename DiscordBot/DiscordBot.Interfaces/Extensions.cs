namespace DiscordBot.Interfaces
{
    using Microsoft.Extensions.Configuration;

    public static class Extensions
    {
        public static string GetAppSetting(this IConfiguration configuration, string key)
        {
            return configuration.GetSection("AppSettings")[key];
        }
    }
}