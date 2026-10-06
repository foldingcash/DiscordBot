namespace DiscordBot.Core
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    internal class BotModule : ModuleBase<SocketCommandContext>
    {
        private static readonly SemaphoreSlim LongRunningLock = new SemaphoreSlim(1, 1);

        private static readonly Emoji Hourglass = new Emoji("⏳");

        private readonly IOptionsMonitor<BotSettings> botSettingsMonitor;

        private readonly ILogger logger;

        public BotModule(ILogger logger, IOptionsMonitor<BotSettings> botSettingsMonitor)
        {
            this.logger = logger;
            this.botSettingsMonitor = botSettingsMonitor;
        }

        private BotSettings BotSettings => botSettingsMonitor.CurrentValue;

        protected async Task Announce(string message, string announceGuild, string announceChannel)
        {
            SocketGuild guild = Context.Client.Guilds.FirstOrDefault(g => g.Name == announceGuild);
            SocketTextChannel channel = guild?.TextChannels.FirstOrDefault(c => c.Name == announceChannel);

            if (channel is null)
            {
                logger.LogWarning("Unable to find the announce channel {guild}/{channel}", announceGuild,
                    announceChannel);
                return;
            }

            await channel.SendMessageAsync(message);
        }

        protected Task Reply(string message, [CallerMemberName] string methodName = "")
        {
            return Reply(() => Task.FromResult(message), methodName);
        }

        protected async Task Reply(Func<Task<string>> getMessage, [CallerMemberName] string methodName = "")
        {
            try
            {
                logger.LogInformation("Method Invoked: {methodName}", methodName);

                await Context.Message.AddReactionAsync(Hourglass);

                await ReplyAsync(await getMessage.Invoke());

                logger.LogInformation("Method Finished: {methodName}", methodName);
            }
            catch (Exception ex)
            {
                await SendAdminMessage(
                    $"There was an exception while replying to '{methodName}'.{Environment.NewLine}{Environment.NewLine}{ex}");
                logger.LogError(ex, "There was an unhandled exception while replying");
            }
            finally
            {
                try
                {
                    await Context.Message.RemoveReactionAsync(Hourglass, Context.Client.CurrentUser);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "There was an error removing the reaction");
                }
            }
        }

        /// <summary>
        ///     Replies with the result of a long running request, only allowing one such request at a time.
        /// </summary>
        protected async Task ReplyAsyncMode(Func<Task<string>> getMessage, [CallerMemberName] string methodName = "")
        {
            if (!await LongRunningLock.WaitAsync(0))
            {
                await Reply("Wait until the bot has finished responding to another user's long running request.",
                    methodName);
                return;
            }

            try
            {
                await Reply(getMessage, methodName);
            }
            finally
            {
                LongRunningLock.Release();
            }
        }

        private async Task SendAdminMessage(string message)
        {
            try
            {
                if (message.Length > DiscordConfig.MaxMessageSize)
                {
                    message = message.Substring(0, DiscordConfig.MaxMessageSize);
                }

                IUser user = await Context.Client.GetUserAsync(BotSettings.AdminUser);
                await user.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send admin message");
            }
        }
    }
}
