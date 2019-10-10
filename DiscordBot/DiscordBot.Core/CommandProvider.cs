namespace DiscordBot.Core
{
    using System;
    using System.Threading.Tasks;

    using Discord.Commands;

    using global::DiscordBot.Core.Modules;

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
            return innerService.AddModuleAsync<DiscordBotModule>(services);
        }

        public async Task<IResult> ExecuteAsync(SocketCommandContext commandContext, int argumentPosition)
        {
            if (IsDevelopmentEnvironment() && commandContext.Channel.Name != GetDevChannel())
            {
                return ExecuteResult.FromSuccess();
            }

            return await innerService.ExecuteAsync(commandContext, argumentPosition, services);
        }

        private string GetDevChannel()
        {
            return configuration.GetSection("AppSettings")["DevChannel"];
        }

        private bool IsDevelopmentEnvironment()
        {
            bool parsed = bool.TryParse(configuration.GetSection("AppSettings")["DevMode"], out bool devMode);
            return parsed && devMode;
        }
    }
}