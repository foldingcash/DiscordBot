namespace DiscordBot.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Attributes;
    using Discord.Commands;
    using FoldingBot;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using TestingBot;

    public class CommandProvider : ICommandService
    {
        private readonly IBotConfigurationService botConfigurationService;

        private readonly IHostEnvironment environment;

        private readonly IOptionsMonitor<FoldingBotSettings> foldingBotSettingsMonitor;

        private readonly CommandService innerService;

        private readonly ILogger logger;

        private readonly IServiceProvider services;

        public CommandProvider(ILogger<CommandProvider> logger, IServiceProvider services,
            IOptionsMonitor<FoldingBotSettings> foldingBotSettingsMonitor, IHostEnvironment environment,
            IBotConfigurationService botConfigurationService)
        {
            innerService = new CommandService(new CommandServiceConfig());
            innerService.Log += logger.LogDiscordMessage;

            this.logger = logger;
            this.services = services;
            this.foldingBotSettingsMonitor = foldingBotSettingsMonitor;
            this.environment = environment;
            this.botConfigurationService = botConfigurationService;
        }

        private FoldingBotSettings FoldingBotSettings =>
            foldingBotSettingsMonitor?.CurrentValue ?? new FoldingBotSettings();

        public async Task AddModulesAsync()
        {
            if (environment.IsDevelopment())
            {
                await innerService.AddModuleAsync<TestingBotModule>(services);
            }

            await innerService.AddModuleAsync<FoldingBotModule>(services);
            await innerService.AddModuleAsync<BaseModule>(services);
        }

        public async Task<IResult> ExecuteAsync(SocketCommandContext commandContext, int argumentPosition)
        {
            if (!IsBotChannel(commandContext))
            {
                return ExecuteResult.FromSuccess();
            }

            SearchResult searchResult = innerService.Search(commandContext, argumentPosition);

            if (!searchResult.IsSuccess)
            {
                return await ExecuteDefaultResponse(commandContext, argumentPosition);
            }

            IReadOnlyList<CommandInfo> matches = searchResult.Commands.Select(match => match.Command).ToList();

            if (matches.Any(command => command.HasAttribute<DevelopmentAttribute>()) && !environment.IsDevelopment())
            {
                return ExecuteResult.FromSuccess();
            }

            if (matches.Any(command => command.HasAttribute<AdminOnlyAttribute>()) && !IsAdminRequesting(commandContext))
            {
                return ExecuteResult.FromSuccess();
            }

            if (matches.Any(command => command.IsDisabled(botConfigurationService)))
            {
                logger.LogDebug("Ignoring disabled command: {command}", matches[0].Name);
                return ExecuteResult.FromSuccess();
            }

            return await innerService.ExecuteAsync(commandContext, argumentPosition, services);
        }

        public async Task<IResult> ExecuteDefaultResponse(SocketCommandContext commandContext, int argumentPosition)
        {
            if (!IsBotChannel(commandContext))
            {
                return ExecuteResult.FromSuccess();
            }

            CommandInfo defaultCommand =
                innerService.Commands.FirstOrDefault(command => command.HasAttribute<DefaultAttribute>());

            if (defaultCommand is null)
            {
                return ExecuteResult.FromSuccess();
            }

            return await defaultCommand.ExecuteAsync(commandContext, Enumerable.Empty<object>(),
                Enumerable.Empty<object>(), services);
        }

        public IEnumerable<CommandInfo> GetCommands(SocketCommandContext context)
        {
            if (IsAdminDirectMessage(context))
            {
                return innerService.Commands.Where(command =>
                    !command.HasAttribute<DevelopmentAttribute>() || environment.IsDevelopment());
            }

            return innerService.Commands.Where(command =>
                !command.HasAttribute<DefaultAttribute>()
                && !command.HasAttribute<HiddenAttribute>()
                && !command.HasAttribute<DevelopmentAttribute>()
                && !command.HasAttribute<DeprecatedAttribute>()
                && !command.HasAttribute<AdminOnlyAttribute>()
                && !command.IsDisabled(botConfigurationService));
        }

        private bool IsAdminDirectMessage(SocketCommandContext commandContext)
        {
            return commandContext.IsPrivate && IsAdminRequesting(commandContext);
        }

        private bool IsAdminRequesting(SocketCommandContext commandContext)
        {
            return FoldingBotSettings.AdminUser == commandContext.Message.Author.Id;
        }

        private bool IsBotChannel(SocketCommandContext commandContext)
        {
            return commandContext.IsPrivate || commandContext.Channel.Name == FoldingBotSettings.BotChannel;
        }
    }
}
