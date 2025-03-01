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

            this.logger = logger;
            this.services = services;
            this.foldingBotSettingsMonitor = foldingBotSettingsMonitor;
            this.environment = environment;
            this.botConfigurationService = botConfigurationService;
        }

        private FoldingBotSettings foldingBotSettings =>
            foldingBotSettingsMonitor?.CurrentValue ?? new FoldingBotSettings();

        public async Task AddModulesAsync()
        {
            if (environment.IsDevelopment())
            {
                await innerService.AddModuleAsync<TestingBotModule>(services);
            }

            await Task.WhenAll(innerService.AddModuleAsync<FoldingBotModule>(services),
                innerService.AddModuleAsync<BaseModule>(services));
        }

        public async Task<IResult> ExecuteAsync(SocketCommandContext commandContext, int argumentPosition)
        {
            if (!commandContext.IsPrivate && commandContext.Channel.Name != GetBotChannel())
            {
                return ExecuteResult.FromSuccess();
            }

            SearchResult searchResult = innerService.Search(commandContext, argumentPosition);

            if (!searchResult.IsSuccess)
            {
                return await ExecuteDefaultResponse(commandContext, argumentPosition);
            }

            bool matchIsDevCommand = searchResult.Commands.Any(command =>
                command.Command.Attributes.Any(attribute => attribute is DevelopmentAttribute));

            if (matchIsDevCommand && !environment.IsDevelopment())
            {
                return ExecuteResult.FromSuccess();
            }

            bool matchIsAdminCommand = searchResult.Commands.Any(command =>
                command.Command.Attributes.Any(attribute => attribute is AdminOnlyAttribute));

            if (matchIsAdminCommand && !IsAdminRequesting(commandContext))
            {
                return ExecuteResult.FromSuccess();
            }

            return await innerService.ExecuteAsync(commandContext, argumentPosition, services);
        }

        public async Task<IResult> ExecuteDefaultResponse(SocketCommandContext commandContext, int argumentPosition)
        {
            if (!commandContext.IsPrivate && commandContext.Channel.Name != GetBotChannel())
            {
                return ExecuteResult.FromSuccess();
            }

            CommandInfo defaultCommand = innerService.Commands.FirstOrDefault(command =>
                command.Attributes.Any(attribute => attribute is DefaultAttribute));

            if (defaultCommand is default(CommandInfo))
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
                    command.Attributes.All(attribute =>
                        !(attribute is DevelopmentAttribute)) || environment.IsDevelopment());
            }

            return innerService.Commands
                               .Where(command =>
                                   command.Attributes.All(attribute =>
                                       !(attribute is DefaultAttribute)))
                               .Where(command =>
                                   command.Attributes.All(attribute =>
                                       !(attribute is HiddenAttribute)))
                               .Where(command =>
                                   command.Attributes.All(attribute =>
                                       !(attribute is DevelopmentAttribute)))
                               .Where(command =>
                                   command.Attributes.All(attribute =>
                                       !(attribute is DeprecatedAttribute)))
                               .Where(command =>
                                   command.Attributes.All(attribute =>
                                       !(attribute is AdminOnlyAttribute)))
                               .Where(command =>
                                   !botConfigurationService.DisabledCommandsContains(
                                       command.Name));
        }

        private string GetBotChannel()
        {
            return foldingBotSettings.BotChannel;
        }

        private bool IsAdminDirectMessage(SocketCommandContext commandContext)
        {
            return string.Equals(foldingBotSettings.AdminUser, commandContext.Message.Author.Username,
                StringComparison.OrdinalIgnoreCase) && commandContext.IsPrivate;
        }

        private bool IsAdminRequesting(SocketCommandContext commandContext)
        {
            return string.Equals(foldingBotSettings.AdminUser, commandContext.Message.Author.Username,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}