namespace DiscordBot.Core
{
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;

    public class BotConfigurationProvider<T> : IBotConfigurationService where T : BotConfiguration, new()
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private readonly IOptionsMonitor<BotSettings> botSettingsMonitor;

        private readonly SemaphoreSlim writeLock = new SemaphoreSlim(1, 1);

        protected T configuration = new T();

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
            var configuration = new T();
            if (File.Exists(ConfigurationPath))
            {
                string contents = await File.ReadAllTextAsync(ConfigurationPath);
                if (!string.IsNullOrWhiteSpace(contents))
                {
                    configuration = JsonSerializer.Deserialize<T>(contents) ?? configuration;
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
            await writeLock.WaitAsync();
            try
            {
                string data = JsonSerializer.Serialize(configuration, SerializerOptions);
                await File.WriteAllTextAsync(ConfigurationPath, data);
            }
            finally
            {
                writeLock.Release();
            }
        }
    }
}
