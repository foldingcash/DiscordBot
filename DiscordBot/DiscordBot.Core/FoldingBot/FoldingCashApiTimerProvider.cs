namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
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

        private readonly IHttpClientFactory httpFactory;

        private readonly ILogger logger;

        private Timer cooldown;

        private Timer timer;

        public FoldingCashApiTimerProvider(ILogger<FoldingCashApiTimerProvider> logger, IHttpClientFactory httpFactory,
            IOptions<BotSettings> botSetttings,
            DiscordSocketClient client)
        {
            this.logger = logger;
            this.httpFactory = httpFactory;
            this.botSetttings = botSetttings;
            this.client = client;

            timer = new Timer
            {
                AutoReset = true,
                Interval = 10000, // 10 seconds
            };
            timer.Elapsed += Elapsed;

            cooldown = new Timer
            {
                AutoReset = false,
                Enabled = false,
                Interval = 3600000 // 1 hour
            };
            cooldown.Elapsed += Cooldown;
        }

        public void Close()
        {
            timer.Close();
        }

        public void Dispose()
        {
            timer.Dispose();
            timer = null;

            cooldown.Dispose();
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

                HealthResponse response = await HealthCheck();

                if (response == null ||
                    !string.Equals(response.Status, "Healthy", StringComparison.CurrentCultureIgnoreCase))
                {
                    await admin.SendMessageAsync("Bro....the API is down.");
                    timer.Stop();
                    cooldown.Start();
                }
            }
        }

        private async Task<HealthResponse> HealthCheck()
        {
            try
            {
                var requestUri = new Uri("health/details", UriKind.Relative);

                var serializer = new DataContractJsonSerializer(typeof (HealthResponse));

                using HttpClient client = httpFactory.CreateClient(ClientTypes.FoldingCashApi);

                logger.LogDebug("Starting GET from URI: {URI}", requestUri.ToString());

                HttpResponseMessage httpResponse = await client.GetAsync(requestUri);

                logger.LogDebug("Finished GET from URI");

                string responseContent = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    logger.LogError("The response status code: {statusCode} responseContent: {responseContent}",
                        httpResponse.StatusCode, responseContent);

                    return null;
                }

                logger.LogTrace("responseContent: {responseContent}", responseContent);

                await using var streamReader = new MemoryStream(Encoding.UTF8.GetBytes(responseContent));

                var response = serializer.ReadObject(streamReader) as HealthResponse;

                return response;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "There was an exception while attempting to get the API's health");
                return null;
            }
        }
    }
}