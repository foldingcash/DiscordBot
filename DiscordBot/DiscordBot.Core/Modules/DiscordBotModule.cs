namespace DiscordBot.Core.Modules
{
    using System.Threading.Tasks;

    using Discord.Commands;

    public class DiscordBotModule : ModuleBase<SocketCommandContext>
    {
        [Command("")]
        public async Task Help()
        {
            await ReplyAsync("");
        }
    }
}