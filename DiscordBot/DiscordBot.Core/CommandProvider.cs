namespace DiscordBot.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord.Commands;

    using DiscordBot.Core.Modules;
    using DiscordBot.Interfaces;

    using Microsoft.Extensions.Configuration;

    public class CommandProvider : ICommandService
    {
        private readonly IConfiguration configuration;

        private readonly CommandService innerService;

        private readonly IServiceProvider services;

        public CommandProvider(IServiceProvider services, IConfiguration configuration)
        {
            innerService = new CommandService(new CommandServiceConfig());

            this.services = services;
            this.configuration = configuration;
        }

        public Task AddModulesAsync()
        {
            return innerService.AddModuleAsync<FoldingBotModule>(services);
        }

        public Task<IResult> ExecuteAsync(SocketCommandContext commandContext, int argumentPosition)
        {
            if (IsDevelopmentEnvironment() && commandContext.Channel.Name != GetDevChannel())
            {
                return Task.Run(() => ExecuteResult.FromSuccess() as IResult);
            }

            return innerService.ExecuteAsync(commandContext, argumentPosition, services);
        }

        public IEnumerable<CommandInfo> GetCommands()
        {
            return innerService.Commands.Where(command => command.Name != "good bot" && command.Name != "bad bot");
        }

        private string GetDevChannel()
        {
            return configuration.GetAppSetting("DevChannel");
        }

        private bool IsDevelopmentEnvironment()
        {
            bool parsed = bool.TryParse(configuration.GetAppSetting("DevMode"), out bool devMode);
            return parsed && devMode;
        }
    }
}