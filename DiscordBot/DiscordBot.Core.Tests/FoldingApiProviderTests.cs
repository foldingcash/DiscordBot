namespace DiscordBot.Core.Tests
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Threading.Tasks;
    using Core.FoldingBot;
    using Core.FoldingBot.Models;
    using Microsoft.Extensions.Logging.Abstractions;

    public class FoldingApiProviderTests
    {
        private static FoldingApiProvider CreateProvider(FakeHttpMessageHandler handler)
        {
            return new FoldingApiProvider(NullLogger<FoldingApiProvider>.Instance, new FakeHttpClientFactory(handler));
        }

        [Fact]
        public async Task GetDistro_DeserializesTheResponse()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Json(
                @"{""success"":true,""distroCount"":1,""startDateTime"":""2026-10-01T00:00:00"",
                   ""endDateTime"":""2026-10-05T00:00:00"",""totalPoints"":1234,
                   ""distro"":[{""cashTokensAddress"":""bitcoincash:abc"",""pointsGained"":1234,""amount"":100}]}");

            DistroResponse response = await CreateProvider(handler)
                .GetDistro(new DateTime(2026, 10, 1), new DateTime(2026, 10, 5), 100);

            Assert.True(response.Success);
            Assert.Equal(1234, response.TotalPoints);
            Assert.Equal(new DateTime(2026, 10, 5), response.End);
            Assert.Equal("bitcoincash:abc", Assert.Single(response.Distro).CashTokensAddress);
        }

        [Fact]
        public async Task GetDistro_FormatsDatesIndependentOfCulture()
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            try
            {
                FakeHttpMessageHandler handler = FakeHttpMessageHandler.Json("{}");

                await CreateProvider(handler).GetDistro(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), 100);

                Uri request = Assert.Single(handler.Requests);
                Assert.Equal(
                    "?startDate=09%2f01%2f2026&endDate=09%2f30%2f2026&amount=100&includeFoldingUserTypes=8",
                    request.Query);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Fact]
        public async Task HealthCheck_ReturnsNullOnFailureWithoutRetrying()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Json("{}", HttpStatusCode.ServiceUnavailable);

            HealthResponse response = await CreateProvider(handler).HealthCheck();

            Assert.Null(response);
            Assert.False(response.IsHealthy());
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task HealthCheck_IsHealthyIgnoresCase()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Json(@"{""status"":""healthy""}");

            HealthResponse response = await CreateProvider(handler).HealthCheck();

            Assert.True(response.IsHealthy());
        }
    }
}
