namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Extensions.Logging;
    using Models;

    public class FoldingApiProvider : IFoldingApiService
    {
        private const string ApiDateFormat = "MM/dd/yyyy";

        private const int DefaultRetryAttempts = 3;

        private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(5);

        private static readonly JsonSerializerOptions SerializerOptions =
            new JsonSerializerOptions(JsonSerializerDefaults.Web);

        private readonly IHttpClientFactory httpFactory;

        private readonly ILogger<FoldingApiProvider> logger;

        public FoldingApiProvider(ILogger<FoldingApiProvider> logger, IHttpClientFactory httpFactory)
        {
            this.logger = logger;
            this.httpFactory = httpFactory;
        }

        public Task<MembersResponse> GetAllMembers()
        {
            var endpoint = new Uri("v1/GetMembers/All", UriKind.Relative);
            return CallApi<MembersResponse>(endpoint, DefaultRetryAttempts);
        }

        public Task<DistroResponse> GetDistro(DateTime startDate, DateTime endDate, int amount)
        {
            const int cashTokenUsers = 8;
            NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
            query.Add("startDate", startDate.ToString(ApiDateFormat, CultureInfo.InvariantCulture));
            query.Add("endDate", endDate.ToString(ApiDateFormat, CultureInfo.InvariantCulture));
            query.Add("amount", amount.ToString(CultureInfo.InvariantCulture));
            query.Add("includeFoldingUserTypes", cashTokenUsers.ToString(CultureInfo.InvariantCulture));
            var endpoint = new Uri($"v1/GetDistro?{query}", UriKind.Relative);

            return CallApi<DistroResponse>(endpoint, DefaultRetryAttempts);
        }

        public Task<HealthResponse> HealthCheck()
        {
            var endpoint = new Uri("health/details", UriKind.Relative);
            return CallApi<HealthResponse>(endpoint);
        }

        private static bool IsTimeout(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.BadGateway || statusCode == HttpStatusCode.GatewayTimeout
                                                           || statusCode == HttpStatusCode.RequestTimeout;
        }

        private async Task<T> CallApi<T>(Uri endpoint, int retryAttempts = 0)
        {
            for (var attempt = 0;; attempt++)
            {
                bool canRetry = attempt < retryAttempts;

                try
                {
                    using HttpClient client = httpFactory.CreateClient(ClientTypes.FoldingCashApi);

                    logger.LogDebug("Starting GET from URI: {URI}", endpoint);

                    using HttpResponseMessage httpResponse = await client.GetAsync(endpoint);

                    logger.LogDebug("Finished GET from URI");

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        if (logger.IsEnabled(LogLevel.Trace))
                        {
                            string content = await httpResponse.Content.ReadAsStringAsync();
                            logger.LogTrace("responseContent: {responseContent}", content);
                        }

                        return await httpResponse.Content.ReadFromJsonAsync<T>(SerializerOptions);
                    }

                    string responseContent = await httpResponse.Content.ReadAsStringAsync();
                    logger.LogError("The response status code: {statusCode} responseContent: {responseContent}",
                        httpResponse.StatusCode, responseContent);

                    if (!IsTimeout(httpResponse.StatusCode) || !canRetry)
                    {
                        return default;
                    }
                }
                catch (TaskCanceledException exception) when (canRetry)
                {
                    logger.LogWarning(exception, "The request timed out");
                }

                // The client and response are disposed before waiting to retry
                logger.LogDebug("Going to attempt to download again after sleeping");
                await Task.Delay(RetryDelay);
            }
        }
    }
}
