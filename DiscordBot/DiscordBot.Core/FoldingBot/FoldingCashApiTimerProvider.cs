namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.Timers;
    using Discord;
    using Discord.WebSocket;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Models;

    public class FoldingCashApiTimerProvider : IBotTimerService
    {
        private readonly IOptions<BotSettings> botSetttings;

        private readonly DiscordSocketClient client;

        private readonly IFoldingApiService foldingApiService;

        private readonly ILogger logger;

        private Timer cooldown;

        private Timer timer;

        public FoldingCashApiTimerProvider(ILogger<FoldingCashApiTimerProvider> logger,
            IOptions<BotSettings> botSetttings,
            IOptions<FoldingCashApiTimerSettings> timerSettings,
            DiscordSocketClient client,
            IFoldingApiService foldingApiService)
        {
            this.logger = logger;
            this.botSetttings = botSetttings;
            this.client = client;
            this.foldingApiService = foldingApiService;

            timer = new Timer
            {
                AutoReset = true,
                Enabled = false,
                Interval = timerSettings.Value.Interval
            };
            timer.Elapsed += Elapsed;

            cooldown = new Timer
            {
                AutoReset = false,
                Enabled = false,
                Interval = timerSettings.Value.CooldownInterval
            };
            cooldown.Elapsed += Cooldown;
        }

        public void Dispose()
        {
            timer?.Dispose();
            timer = null;

            cooldown?.Dispose();
            cooldown = null;
        }

        public void Start()
        {
            timer.Start();
            cooldown.Stop();
        }

        public void Stop()
        {
            timer.Stop();
            cooldown.Stop();
        }

        private void Cooldown(object sender, ElapsedEventArgs e)
        {
            logger.LogInformation("Main timer has cooled down, starting main timer");
            cooldown.Stop();
            timer.Start();
        }

        private async void Elapsed(object sender, ElapsedEventArgs e)
        {
            logger.LogInformation("Main timer elapsed, doing work");
            if (client.ConnectionState == ConnectionState.Connected)
            {
                IUser admin = await client.GetUserAsync(botSetttings.Value.AdminUser);
                if (admin == null)
                {
                    return;
                }

                HealthResponse response = await foldingApiService.HealthCheck();

                if (response == null ||
                    !string.Equals(response.Status, "Healthy", StringComparison.CurrentCultureIgnoreCase))
                {
                    await admin.SendMessageAsync("Bro....the API is down.");
                    timer.Stop();
                    cooldown.Start();
                }
            }
        }
    }
}