namespace DiscordBot.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord.Commands;

    using DiscordBot.Core.FoldingBot;
    using DiscordBot.Interfaces;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class CommandProvider : ICommandService
    {
        private readonly IConfiguration configuration;

        private readonly CommandService innerService;

        private readonly ILogger<CommandProvider> logger;

        private readonly IServiceProvider services;

        public CommandProvider(ILogger<CommandProvider> logger, IServiceProvider services, IConfiguration configuration)
        {
            innerService = new CommandService(new CommandServiceConfig());

            this.logger = logger;
            this.services = services;
            this.configuration = configuration;
        }

        public Task AddModulesAsync()
        {
            return innerService.AddModuleAsync<FoldingBotModule>(services);
        }

        public async Task<IResult> ExecuteAsync(SocketCommandContext commandContext, int argumentPosition)
        {
            if (IsDevelopmentEnvironment() && commandContext.Channel.Name != GetDevChannel())
            {
                return ExecuteResult.FromSuccess();
            }

            SearchResult searchResult = innerService.Search(commandContext, argumentPosition);

            bool matchIsDevCommand = searchResult.Commands.Any(command =>
                command.Command.Attributes.Any(attribute =>
                    attribute is DevelopmentAttribute));

            if (!IsDevelopmentEnvironment() && matchIsDevCommand)
            {
                return ExecuteResult.FromSuccess();
            }

            return await innerService.ExecuteAsync(commandContext, argumentPosition, services);
        }

        public IEnumerable<CommandInfo> GetCommands()
        {
            bool isDevMode = IsDevelopmentEnvironment();

            IEnumerable<CommandInfo> removedHiddenCommands =
                innerService.Commands.Where(command =>
                    command.Attributes.All(attribute => !(attribute is HiddenAttribute)));

            IEnumerable<CommandInfo> removedDevCommands =
                isDevMode ? removedHiddenCommands : removedHiddenCommands.Where(command =>
                    command.Attributes.All(attribute => !(attribute is DevelopmentAttribute)));

            return removedDevCommands;
        }

        private string GetDevChannel()
        {
            return configuration.GetAppSetting("DevChannel");
        }

        private bool IsDevelopmentEnvironment()
        {
            string rawValue = configuration.GetAppSetting("DevMode");
            logger.LogDebug("Attempting to parse DevMode configuration value: {value}", rawValue);
            bool parsed = bool.TryParse(rawValue, out bool devMode);
            return parsed && devMode;
        }
    }
}