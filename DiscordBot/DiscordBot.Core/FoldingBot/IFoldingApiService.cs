namespace DiscordBot.Core.FoldingBot
{
    using System.Threading.Tasks;
    using Models;

    public interface IFoldingApiService
    {
        Task<HealthResponse> HealthCheck();
    }
}