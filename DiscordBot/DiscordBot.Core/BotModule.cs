namespace DiscordBot.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    internal class BotModule : ModuleBase<SocketCommandContext>
    {
        private static readonly object AsyncLock = new object();

        private static bool isRunningAsyncMethod;

        private readonly IBotConfigurationService botConfigurationService;

        private readonly IOptionsMonitor<BotSettings> botSettingsMonitor;

        private readonly Emoji hourglass = new Emoji("\u23F3");

        private readonly ILogger logger;

        public BotModule(ILogger logger,
            IOptionsMonitor<BotSettings> botSettingsMonitor, IBotConfigurationService botConfigurationService)
        {
            this.logger = logger;
            this.botConfigurationService = botConfigurationService;
            this.botSettingsMonitor = botSettingsMonitor;
        }

        private BotSettings BotSettings => botSettingsMonitor.CurrentValue;

        protected async Task Announce(string message, string announceGuild, string announceChannel)
        {
            IReadOnlyCollection<SocketGuild> guilds = Context.Client.Guilds;
            SocketGuild guild = guilds.FirstOrDefault(g => g.Name == announceGuild);
            IReadOnlyCollection<SocketTextChannel> channels = guild?.TextChannels;
            SocketTextChannel channel = channels?.FirstOrDefault(c => c.Name == announceChannel);

            await (channel?.SendMessageAsync(message) ?? Task.CompletedTask);
        }

        protected CommandAttribute GetCommandAttribute([CallerMemberName] string methodName = "")
        {
            return GetType().GetMethod(methodName)?.GetCustomAttributes(true).OfType<CommandAttribute>()
                            .FirstOrDefault();
        }

        protected async Task Reply(string message, [CallerMemberName] string methodName = "")
        {
            await Reply(() => Task.FromResult(message), methodName);
        }

        protected async Task Reply(Func<Task<string>> getMessage, [CallerMemberName] string methodName = "")
        {
            CommandAttribute commandAttribute = GetCommandAttribute(methodName);

            if (botConfigurationService.DisabledCommandsContains(commandAttribute?.Text))
            {
                return;
            }

            try
            {
                logger.LogInformation("Method Invoked: {methodName}", methodName);

                await Context.Message.AddReactionAsync(hourglass);

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
                    await Context.Message.RemoveReactionAsync(hourglass, Context.Client.CurrentUser,
                        RequestOptions.Default);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "There was an error removing the reaction");
                }
            }
        }

        protected async Task ReplyAsyncMode(Func<Task<string>> getMessage, [CallerMemberName] string methodName = "")
        {
            var runAsync = true;

            lock (AsyncLock)
            {
                if (isRunningAsyncMethod)
                {
                    runAsync = false;
                }
                else
                {
                    isRunningAsyncMethod = true;
                }
            }

            if (runAsync)
            {
                await Reply(getMessage, methodName);

                isRunningAsyncMethod = false;
            }
            else
            {
                await Reply("Wait until the bot has finished responding to another user's long running request.");
            }
        }

        private async Task SendAdminMessage(string message)
        {
            try
            {
                var user = await Context.Client.GetUserAsync(BotSettings.AdminUser);
                await user.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send admin message");
            }
        }
    }
}