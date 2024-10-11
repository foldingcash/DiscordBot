using Microsoft.Extensions.Options;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordBot.Core
{
    public class BotConfigurationProvider<T> : IBotConfigurationService where T : BotConfiguration, new()
    {
        private readonly IOptionsMonitor<BotSettings> botSettingsMonitor;
        protected T configuration;

        public BotConfigurationProvider(IOptionsMonitor<BotSettings> botSettingsMonitor)
        {
            this.botSettingsMonitor = botSettingsMonitor;
        }

        private BotSettings BotSettings => botSettingsMonitor.CurrentValue;
        private string ConfigurationPath => BotSettings.ConfigurationPath;

        public Task AddDisabledCommands(string commandName)
        {
            if (configuration.DisabledCommands.Contains(commandName))
            {
                return Task.CompletedTask;
            }
            configuration.DisabledCommands.Add(commandName);
            return WriteConfiguration();
        }

        public bool DisabledCommandsContains(string name)
        {
            return configuration.DisabledCommands.Contains(name);
        }

        public async Task ReadConfiguration()
        {
            T configuration = new T();
            if (File.Exists(ConfigurationPath))
            {
                using var read = File.OpenRead(ConfigurationPath);
                using var reader = new StreamReader(ConfigurationPath);
                var contents = await reader.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(contents))
                {
                    read.Position = 0;
                    configuration = await JsonSerializer.DeserializeAsync<T>(read);
                }
            }

            this.configuration = configuration;
        }

        public Task RemoveDisabledCommands(string commandName)
        {
            configuration.DisabledCommands.Remove(commandName);
            return WriteConfiguration();
        }

        protected async Task WriteConfiguration()
        {
            using var memory = new MemoryStream();
            await JsonSerializer.SerializeAsync(memory, configuration);
            memory.Position = 0;
            using var reader = new StreamReader(memory);

            var data = await reader.ReadToEndAsync();

            await File.WriteAllTextAsync(ConfigurationPath, data);
        }
    }
}
