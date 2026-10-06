namespace DiscordBot.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.FoldingBot;
    using Core.FoldingBot.Models;
    using Microsoft.Extensions.Options;

    internal class FakeFoldingApiService : IFoldingApiService
    {
        public DistroResponse Distro { get; set; } = new DistroResponse
        {
            StartDateTime = "2026-10-01T00:00:00",
            EndDateTime = "2026-10-05T00:00:00",
            Distro = new List<DistroUser>()
        };

        public HealthResponse Health { get; set; }

        public (DateTime StartDate, DateTime EndDate)? LastDistroRequest { get; private set; }

        public MembersResponse Members { get; set; }

        public Task<MembersResponse> GetAllMembers()
        {
            return Task.FromResult(Members);
        }

        public Task<DistroResponse> GetDistro(DateTime startDate, DateTime endDate, int amount)
        {
            LastDistroRequest = (startDate, endDate);
            return Task.FromResult(Distro);
        }

        public Task<HealthResponse> HealthCheck()
        {
            return Task.FromResult(Health);
        }
    }

    internal class FakeFoldingBotConfigurationService : IFoldingBotConfigurationService
    {
        public DateTime? DistroDate { get; set; }

        public Task AddDisabledCommands(string commandName)
        {
            return Task.CompletedTask;
        }

        public Task ClearDistroDate()
        {
            DistroDate = null;
            return Task.CompletedTask;
        }

        public bool DisabledCommandsContains(string name)
        {
            return false;
        }

        public DateTime? GetDistroDate()
        {
            return DistroDate;
        }

        public Task ReadConfiguration()
        {
            return Task.CompletedTask;
        }

        public Task RemoveDisabledCommands(string commandName)
        {
            return Task.CompletedTask;
        }

        public async Task UpdateDistroDate(DateTime date)
        {
            // Yield so a caller that forgets to await sees the old value
            await Task.Yield();
            DistroDate = date;
        }
    }

    internal class FakeOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public FakeOptionsMonitor(T value)
        {
            CurrentValue = value;
        }

        public T CurrentValue { get; }

        public T Get(string name)
        {
            return CurrentValue;
        }

        public IDisposable OnChange(Action<T, string> listener)
        {
            return null;
        }
    }

    internal class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> respond;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            this.respond = respond;
        }

        public List<Uri> Requests { get; } = new List<Uri>();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri);
            return Task.FromResult(respond(request));
        }

        public static FakeHttpMessageHandler Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new FakeHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    internal class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler handler;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            this.handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(handler, false)
            {
                BaseAddress = new Uri("https://api.folding.test/")
            };
        }
    }
}
