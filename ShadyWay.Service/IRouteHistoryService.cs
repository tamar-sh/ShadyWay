using ShadyWay.Core.Models;

namespace ShadyWay.Service
{
    public interface IRouteHistoryService
    {
        Task<List<RouteHistoryItem>> GetHistoryAsync(int userId);
    }
}
