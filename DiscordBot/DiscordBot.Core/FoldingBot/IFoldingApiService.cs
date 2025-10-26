namespace DiscordBot.Core.FoldingBot
{
    using System;
    using System.Threading.Tasks;
    using Models;

    public interface IFoldingApiService
    {
        Task<MembersResponse> GetAllMembers();

        Task<DistroResponse> GetDistro(DateTime startDate, DateTime endDate, int amount);

        Task<HealthResponse> HealthCheck();
    }
}