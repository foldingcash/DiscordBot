namespace DiscordBot.Core
{
    using System.Threading.Tasks;
    using Discord;
    using Microsoft.Extensions.Logging;

    internal static class DiscordLogExtensions
    {
        public static Task LogDiscordMessage(this ILogger logger, LogMessage message)
        {
            LogLevel level = message.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Debug,
                _ => LogLevel.Trace
            };

            logger.Log(level, message.Exception, "[{source}] {message}", message.Source, message.Message);
            return Task.CompletedTask;
        }
    }
}
