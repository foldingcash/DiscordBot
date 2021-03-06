namespace DiscordBot.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord.Commands;

    using DiscordBot.Core.Attributes;
    using DiscordBot.Core.FoldingBot;
    using DiscordBot.Core.Interfaces;
    using DiscordBot.Core.TestingBot;

    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class CommandProvider : ICommandService
    {
        private readonly IHostEnvironment environment;

        private readonly IOptionsMonitor<FoldingBotConfig> foldingBotConfigMonitor;

        private readonly CommandService innerService;

        private readonly ILogger logger;

        private readonly IServiceProvider services;

        public CommandProvider(ILogger<CommandProvider> logger, IServiceProvider services,
                               IOptionsMonitor<FoldingBotConfig> foldingBotConfigMonitor, IHostEnvironment environment)
        {
            innerService = new CommandService(new CommandServiceConfig());

            this.logger = logger;
            this.services = services;
            this.foldingBotConfigMonitor = foldingBotConfigMonitor;
            this.environment = environment;
        }

        private FoldingBotConfig foldingBotConfig => foldingBotConfigMonitor?.CurrentValue ?? new FoldingBotConfig();

        public async Task AddModulesAsync()
        {
            if (environment.IsDevelopment())
            {
                await innerService.AddModuleAsync<TestingBotModule>(services);
            }

            await Task.WhenAll(innerService.AddModuleAsync<FoldingBotModule>(services));
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

            if (matchIsDevCommand && !IsDevelopmentEnvironment())
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

        public IEnumerable<CommandInfo> GetCommands()
        {
            bool isDevMode = IsDevelopmentEnvironment();

            return isDevMode ? innerService.Commands : innerService.Commands
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
                                                                       !RuntimeChanges.DisabledCommands.Contains(
                                                                           command.Name));
        }

        private string GetBotChannel()
        {
            return foldingBotConfig.BotChannel;
        }

        private bool IsAdminRequesting(SocketCommandContext commandContext)
        {
            return string.Equals(foldingBotConfig.AdminUser, commandContext.Message.Author.Username,
                StringComparison.OrdinalIgnoreCase);
        }

        private bool IsDevelopmentEnvironment()
        {
            return foldingBotConfig.DevMode;
        }
    }
}