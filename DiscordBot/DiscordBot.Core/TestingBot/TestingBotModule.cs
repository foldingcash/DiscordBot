namespace DiscordBot.Core.TestingBot
{
    using System.Threading.Tasks;

    using Discord.Commands;

    using DiscordBot.Core.Attributes;

    using Microsoft.Extensions.Logging;

    internal class TestingBotModule : BotModule
    {
        private readonly ILogger logger;

        public TestingBotModule(ILogger<TestingBotModule> logger)
            : base(logger)
        {
            this.logger = logger;
        }

        [AdminOnly]
        [Development]
        [Command("test admin")]
        [Summary("Tests an admin only call")]
        public async Task TestAdmin()
        {
            logger.LogDebug("Testing an admin call");
            await Reply("ACK testing bot");
        }

        [Development]
        [Command("test async", RunMode = RunMode.Async)]
        [Usage("{timeout in seconds defaults to 60 secs}")]
        [Summary("Test long running async methods")]
        public async Task TestAsync(int timeout = 60)
        {
            logger.LogDebug("Testing async with timeout {timeout}", timeout);
            await ReplyAsyncMode(async () =>
            {
                await Task.Delay(timeout * 1000);
                return "Async test finished";
            });
        }
    }
}