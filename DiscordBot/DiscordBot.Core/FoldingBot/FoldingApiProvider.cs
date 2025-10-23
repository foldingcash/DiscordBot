namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Extensions.Logging;
    using Models;

    public class FoldingApiProvider : IFoldingApiService
    {
        private const string ApiDateFormat = "MM/dd/yyyy";

        private readonly IHttpClientFactory httpFactory;

        private readonly ILogger<FoldingApiProvider> logger;

        public FoldingApiProvider(ILogger<FoldingApiProvider> logger, IHttpClientFactory httpFactory)
        {
            this.logger = logger;
            this.httpFactory = httpFactory;
        }

        public async Task<MembersResponse> GetAllMembers()
        {
            var endpoint = new Uri("v1/GetMembers/All", UriKind.Relative);
            var response = await CallApi<MembersResponse>(endpoint, 3);
            return response;
        }

        public async Task<DistroResponse>
            GetDistro(DateTime startDate, DateTime endDate, int amount)
        {
            const int cashTokenUsers = 8;
            NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
            query.Add("startDate", startDate.ToString(ApiDateFormat));
            query.Add("endDate", endDate.ToString(ApiDateFormat));
            query.Add("amount", amount.ToString());
            query.Add("includeFoldingUserTypes", cashTokenUsers.ToString());
            var endpoint =
                new Uri(
                    $"v1/GetDistro?{query}",
                    UriKind.Relative);

            var response = await CallApi<DistroResponse>(endpoint, 3);
            return response;
        }

        public async Task<HealthResponse> HealthCheck()
        {
            var endpoint = new Uri("health/details", UriKind.Relative);
            var response = await CallApi<HealthResponse>(endpoint);
            return response;
        }

        private async Task<T> CallApi<T>(Uri endpoint, int retryAttempts = 0, int sleepInSeconds = 300)
        {
            try
            {
                using HttpClient client = httpFactory.CreateClient(ClientTypes.FoldingCashApi);

                logger.LogDebug("Starting GET from URI: {URI}", endpoint.ToString());

                HttpResponseMessage httpResponse = await client.GetAsync(endpoint);

                logger.LogDebug("Finished GET from URI");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    Stream responseContent = await httpResponse.Content.ReadAsStreamAsync();
                    logger.LogError("The response status code: {statusCode} responseContent: {responseContent}",
                        httpResponse.StatusCode, responseContent);

                    return default;
                }

                if (!httpResponse.IsSuccessStatusCode)
                {
                    string responseContent = await httpResponse.Content.ReadAsStringAsync();
                    logger.LogError("The response status code: {statusCode} responseContent: {responseContent}",
                        httpResponse.StatusCode, responseContent);

                    if (IsTimeout(httpResponse.StatusCode) && retryAttempts > 0)
                    {
                        logger.LogDebug("Going to attempt to download again after sleeping");
                        await Task.Delay(sleepInSeconds * 1000);
                        return await CallApi<T>(endpoint, --retryAttempts);
                    }

                    return default;
                }

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    string responseContent = await httpResponse.Content.ReadAsStringAsync();
                    logger.LogTrace("responseContent: {responseContent}", responseContent);
                }

                Stream contentStream = await httpResponse.Content.ReadAsStreamAsync();
                var response = await JsonSerializer.DeserializeAsync<T>(contentStream,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                return response;
            }
            catch (TaskCanceledException exception)
            {
                if (retryAttempts > 0)
                {
                    logger.LogWarning(exception, "Going to attempt to download again after sleeping");
                    await Task.Delay(sleepInSeconds * 1000);
                    return await CallApi<T>(endpoint, --retryAttempts);
                }

                logger.LogError(exception, "There was an unhandled exception");
                throw;
            }
        }

        private bool IsTimeout(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.BadGateway || statusCode == HttpStatusCode.GatewayTimeout
                                                           || statusCode == HttpStatusCode.RequestTimeout;
        }
    }
}