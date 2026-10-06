namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.Threading;
    using Discord;
    using Discord.WebSocket;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Models;
    using Timer = System.Timers.Timer;
    using ElapsedEventArgs = System.Timers.ElapsedEventArgs;

    public class FoldingCashApiTimerProvider : IBotTimerService
    {
        private readonly IOptions<BotSettings> botSettings;

        private readonly DiscordSocketClient client;

        private readonly Timer cooldown;

        private readonly IFoldingApiService foldingApiService;

        private readonly ILogger logger;

        private readonly Timer timer;

        private int isCheckingHealth;

        public FoldingCashApiTimerProvider(ILogger<FoldingCashApiTimerProvider> logger,
            IOptions<BotSettings> botSettings,
            IOptions<FoldingCashApiTimerSettings> timerSettings,
            DiscordSocketClient client,
            IFoldingApiService foldingApiService)
        {
            this.logger = logger;
            this.botSettings = botSettings;
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
            timer.Dispose();
            cooldown.Dispose();
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
            timer.Start();
        }

        private async void Elapsed(object sender, ElapsedEventArgs e)
        {
            // The health check can take longer than the interval, skip this tick if one is still running
            if (Interlocked.Exchange(ref isCheckingHealth, 1) == 1)
            {
                return;
            }

            try
            {
                logger.LogInformation("Main timer elapsed, doing work");
                if (client.ConnectionState != ConnectionState.Connected)
                {
                    return;
                }

                IUser admin = await client.GetUserAsync(botSettings.Value.AdminUser);
                if (admin == null)
                {
                    return;
                }

                HealthResponse response = await foldingApiService.HealthCheck();

                if (!response.IsHealthy())
                {
                    timer.Stop();
                    cooldown.Start();
                    await admin.SendMessageAsync("Bro....the API is down.");
                }
            }
            catch (Exception ex)
            {
                // Exceptions escaping an async void handler would crash the process
                logger.LogError(ex, "There was an unhandled exception while checking the API health");
            }
            finally
            {
                Interlocked.Exchange(ref isCheckingHealth, 0);
            }
        }
    }
}
