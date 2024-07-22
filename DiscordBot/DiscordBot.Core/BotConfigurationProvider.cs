using Microsoft.Extensions.Options;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordBot.Core
{
    public class BotConfigurationProvider<T> : IBotConfigurationService where T: BotConfiguration, new()
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
            configuration.DisabledCommands.Add(commandName);
            return WriteConfiguration();
        }

        public bool DisabledCommandsContains(string name)
        {
            return configuration.DisabledCommands.Contains(name);
        }

        public async Task ReadConfiguration()
        {
            T configuration;
            if (!File.Exists(ConfigurationPath))
            {
                configuration = new T();
            }
            else
            {
                using var read = File.OpenRead(ConfigurationPath);
                configuration = await JsonSerializer.DeserializeAsync<T>(read);
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
            using var write = File.OpenWrite(ConfigurationPath);
            await JsonSerializer.SerializeAsync(write, configuration);
        }
    }
}
