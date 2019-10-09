namespace DiscordBot.Core
{
    using System;
    using System.Threading.Tasks;

    using Discord.Commands;

    using global::DiscordBot.Core.Modules;

    public class CommandProvider : ICommandService
    {
        private readonly CommandService innerService;

        private readonly IServiceProvider services;

        public CommandProvider(IServiceProvider services)
        {
            innerService = new CommandService(new CommandServiceConfig());

            this.services = services;
        }

        public async Task AddModulesAsync()
        {
            await innerService.AddModuleAsync<DiscordBotModule>(services);
        }

        public Task<IResult> ExecuteAsync(SocketCommandContext commandContext, int argumentPosition)
        {
            throw new NotImplementedException();
        }
    }
}