namespace DiscordBot.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Attributes;
    using Discord.Commands;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    internal class BaseModule : BotModule
    {
        private readonly IBotConfigurationService botConfigurationService;

        private readonly IOptionsMonitor<BotSettings> botSettingsMonitor;

        private readonly ICommandService commandService;

        private readonly ILogger logger;

        public BaseModule(ILogger<BaseModule> logger, IOptionsMonitor<BotSettings> botSettingsMonitor,
            IBotConfigurationService botConfigurationService, ICommandService commandService)
            : base(logger, botSettingsMonitor)
        {
            this.logger = logger;
            this.botSettingsMonitor = botSettingsMonitor;
            this.commandService = commandService;
            this.botConfigurationService = botConfigurationService;
        }

        private BotSettings BotSettings => botSettingsMonitor.CurrentValue;

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
            if (GetCommandNames(nameof(DisableCommand)).Concat(GetCommandNames(nameof(EnableCommand)))
                                                        .Contains(commandName))
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
        public Task Help()
        {
            return Reply(Usage(Context));
        }

        [Default]
        [Hidden]
        [Command("{default}")]
        [Summary("Show the list of available commands")]
        public Task NoCommand()
        {
            return Reply(Usage(Context));
        }

        private static IEnumerable<string> GetCommandNames(string methodName)
        {
            MethodInfo method = typeof(BaseModule).GetMethod(methodName);
            string command = method?.GetCustomAttribute<CommandAttribute>()?.Text;
            string[] aliases = method?.GetCustomAttribute<AliasAttribute>()?.Aliases ?? Array.Empty<string>();
            return aliases.Prepend(command);
        }

        private void AppendAttributeText(CommandInfo command, StringBuilder builder)
        {
            if (command.IsDisabled(botConfigurationService))
            {
                builder.Append("(Disabled) ");
            }

            if (command.HasAttribute<HiddenAttribute>())
            {
                builder.Append("(Hidden) ");
            }

            if (command.HasAttribute<AdminOnlyAttribute>())
            {
                builder.Append("(Admin) ");
            }
        }

        private string Usage(SocketCommandContext context)
        {
            IEnumerable<CommandInfo> commandList = commandService.GetCommands(context)
                                                                 .OrderBy(command => command.Name,
                                                                     StringComparer.CurrentCulture);

            var builder = new StringBuilder();

            builder.AppendLine(
                "To use me, tag me with a command; provide additional information when needed.");
            builder.AppendLine();
            builder.AppendLine($"Usage: @{BotSettings.BotName} {{command}} {{data}}");
            builder.AppendLine();
            builder.AppendLine("Commands -");

            foreach (CommandInfo command in commandList)
            {
                builder.Append("\t");
                AppendAttributeText(command, builder);

                UsageAttribute usageAttribute = command.GetAttribute<UsageAttribute>();
                if (usageAttribute is null)
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
