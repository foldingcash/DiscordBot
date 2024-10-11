namespace DiscordBot.Core
{
    using Discord.Commands;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ICommandService
    {
        Task AddModulesAsync();

        Task<IResult> ExecuteAsync(SocketCommandContext commandContext, int argumentPosition);

        Task<IResult> ExecuteDefaultResponse(SocketCommandContext commandContext, int argumentPosition);

        IEnumerable<CommandInfo> GetCommands(SocketCommandContext context);
    }
}