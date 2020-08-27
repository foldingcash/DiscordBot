namespace DiscordBot.Core.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Discord.Commands;

    public interface ICommandService
    {
        Task AddModulesAsync();

        Task<IResult> ExecuteAsync(SocketCommandContext commandContext, int argumentPosition);

        Task<IResult> ExecuteDefaultResponse(SocketCommandContext commandContext, int argumentPosition);

        IEnumerable<CommandInfo> GetCommands();
    }
}