namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Models;

    public class FoldingApiProvider : IFoldingApiService
    {
        private readonly IHttpClientFactory httpFactory;

        private readonly ILogger<FoldingApiProvider> logger;

        public FoldingApiProvider(ILogger<FoldingApiProvider> logger, IHttpClientFactory httpFactory)
        {
            this.logger = logger;
            this.httpFactory = httpFactory;
        }

        public async Task<HealthResponse> HealthCheck()
        {
            try
            {
                var requestUri = new Uri("health/details", UriKind.Relative);

                using HttpClient client = httpFactory.CreateClient(ClientTypes.FoldingCashApi);

                logger.LogDebug("Starting GET from URI: {URI}", requestUri.ToString());

                HttpResponseMessage httpResponse = await client.GetAsync(requestUri);

                logger.LogDebug("Finished GET from URI");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    Stream responseContent = await httpResponse.Content.ReadAsStreamAsync();
                    logger.LogError("The response status code: {statusCode} responseContent: {responseContent}",
                        httpResponse.StatusCode, responseContent);

                    return null;
                }

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    string responseContent = await httpResponse.Content.ReadAsStringAsync();
                    logger.LogTrace("responseContent: {responseContent}", responseContent);
                }

                Stream contentStream = await httpResponse.Content.ReadAsStreamAsync();
                var healthResponse = await JsonSerializer.DeserializeAsync<HealthResponse>(contentStream,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                return healthResponse;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "There was an exception while attempting to get the API's health");
                return null;
            }
        }
    }
}