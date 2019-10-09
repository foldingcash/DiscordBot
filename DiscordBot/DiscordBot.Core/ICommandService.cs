namespace DiscordBot.Core
{
    using System.Threading.Tasks;

    using Discord.Commands;

    public interface ICommandService
    {
        Task AddModulesAsync();

        Task<IResult> ExecuteAsync(SocketCommandContext commandContext, int argumentPosition);
    }
}