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
        private readonly IOptionsMonitor<BotSettings> botSettingsMonitor;

        private readonly ICommandService commandService;
        private readonly IBotConfigurationService botConfigurationService;
        private readonly ILogger logger;

        public BaseModule(ILogger<BaseModule> logger, IOptionsMonitor<BotSettings> botSettingsMonitor,
                          ICommandService commandService, IBotConfigurationService botConfigurationService)
            : base(logger, botConfigurationService)
        {
            this.logger = logger;
            this.botSettingsMonitor = botSettingsMonitor;
            this.commandService = commandService;
            this.botConfigurationService = botConfigurationService;
        }

        private BotSettings settings => botSettingsMonitor.CurrentValue;

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
        [Alias("disable", "dc")]
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
            await botConfigurationService.AddDisabledCommands(commandName);
            await Reply("Completed");
        }

        [AdminOnly]
        [Hidden]
        [Command("enable command")]
        [Alias("enable", "ec")]
        [Usage("{command name}")]
        [Summary("Enables a specified command")]
        public async Task EnableCommand([Remainder] string commandName)
        {
            logger.LogDebug("Enabling a command...");
            await botConfigurationService.RemoveDisabledCommands(commandName);
            await Reply("Completed");
        }

        [Command("help")]
        [Summary("Show the list of available commands")]
        public async Task Help()
        {
            await Reply(Usage(Context));
        }

        [Default]
        [Hidden]
        [Command("{default}")]
        [Summary("Show the list of available commands")]
        public async Task NoCommand()
        {
            await Reply(Usage(Context));
        }

        private IEnumerable<CommandInfo> GetCommands(SocketCommandContext context)
        {
            List<CommandInfo> commands = commandService.GetCommands(context).ToList();
            commands.Sort((command1, command2) =>
                string.Compare(command1.Name, command2.Name, StringComparison.CurrentCulture));
            return commands;
        }

        private string Usage(SocketCommandContext context)
        {
            IEnumerable<CommandInfo> commandList = GetCommands(context);

            var builder = new StringBuilder();

            builder.AppendLine(
                "To use me, tag me or tell me a command and provide additional information when needed.");
            builder.AppendLine();
            builder.AppendLine("Usage: !{command} {data}");
            builder.AppendLine($"Usage: @{settings.BotName} {{command}} {{data}}");
            builder.AppendLine();
            builder.AppendLine("Commands -");

            foreach (CommandInfo command in commandList)
            {
                var usageAttribute =
                    command.Attributes.FirstOrDefault(attribute => attribute is UsageAttribute) as UsageAttribute;

                builder.Append("\t");

                if(botConfigurationService.DisabledCommandsContains(command.Name))
                {
                    builder.Append("(Disabled) ");
                }

                if (usageAttribute is default(UsageAttribute))
                {
                    builder.Append($"{command.Name} - {command.Summary}");
                }
                else
                {
                    builder.Append($"{command.Name} {usageAttribute.Usage} - {command.Summary}");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}