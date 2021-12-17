namespace DiscordBot.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Discord.Commands;

    using DiscordBot.Core.Attributes;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    internal class BaseModule : BotModule
    {
        private readonly IOptionsMonitor<BotConfig> botConfigMonitor;

        private readonly ICommandService commandService;

        private readonly ILogger logger;

        public BaseModule(ILogger<BaseModule> logger, IOptionsMonitor<BotConfig> botConfigMonitor,
                          ICommandService commandService)
            : base(logger)
        {
            this.logger = logger;
            this.botConfigMonitor = botConfigMonitor;
            this.commandService = commandService;
        }

        private BotConfig config => botConfigMonitor.CurrentValue;

        [Hidden]
        [Command("bad bot")]
        [Summary("Tell the bot it's being bad")]
        public Task AcknowledgeBadBot()
        {
            return Reply("D:");
        }

        [Hidden]
        [Command("good bot")]
        [Summary("Tell the bot it's being good")]
        public Task AcknowledgeGoodBot()
        {
            return Reply(":D");
        }

        [AdminOnly]
        [Hidden]
        [Command("disable command")]
        [Alias("dc")]
        [Usage("{command name}")]
        [Summary("Disables a specified command")]
        public async Task DisableCommand([Remainder] string commandName)
        {
            CommandAttribute command = GetCommandAttribute();
            if (commandName == command.Text)
            {
                logger.LogWarning("Disabling this command is not recommended...");
                return;
            }

            command = GetCommandAttribute(nameof(EnableCommand));
            if (commandName == command.Text)
            {
                logger.LogWarning("Disabling this command is not recommended...");
                return;
            }

            logger.LogDebug("Disabling a command...");
            RuntimeChanges.DisabledCommands.Add(commandName);
            await Reply("Completed");
        }

        [AdminOnly]
        [Hidden]
        [Command("enable command")]
        [Alias("ec")]
        [Usage("{command name}")]
        [Summary("Enables a specified command")]
        public async Task EnableCommand([Remainder] string commandName)
        {
            logger.LogDebug("Enabling a command...");
            RuntimeChanges.DisabledCommands.Remove(commandName);
            await Reply("Completed");
        }

        [Command("help")]
        [Summary("Show the list of available commands")]
        public async Task Help()
        {
            await Reply(Usage());
        }

        [Default]
        [Hidden]
        [Command("{default}")]
        [Summary("Show the list of available commands")]
        public async Task NoCommand()
        {
            await Reply(Usage());
        }

        private IEnumerable<CommandInfo> GetCommands()
        {
            List<CommandInfo> commands = commandService.GetCommands().ToList();
            commands.Sort((command1, command2) =>
                string.Compare(command1.Name, command2.Name, StringComparison.CurrentCulture));
            return commands;
        }

        private string Usage()
        {
            IEnumerable<CommandInfo> commandList = GetCommands();

            var builder = new StringBuilder();

            builder.AppendLine(
                "To use me, tag me or tell me a command and provide additional information when needed.");
            builder.AppendLine();
            builder.AppendLine("Usage: !{command} {data}");
            builder.AppendLine($"Usage: @{config.BotName} {{command}} {{data}}");
            builder.AppendLine();
            builder.AppendLine("Commands -");

            foreach (CommandInfo command in commandList)
            {
                var usageAttribute =
                    command.Attributes.FirstOrDefault(attribute => attribute is UsageAttribute) as UsageAttribute;

                if (usageAttribute is default(UsageAttribute))
                {
                    builder.AppendLine($"\t{command.Name} - {command.Summary}");
                }
                else
                {
                    builder.AppendLine($"\t{command.Name} {usageAttribute.Usage} - {command.Summary}");
                }
            }

            return builder.ToString();
        }
    }
}