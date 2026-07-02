using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShadyWay.Core.Models;
using ShadyWay.Infrastructure;

namespace ShadyWay.Service
{
    public class RouteHistoryService : IRouteHistoryService
    {
        private readonly ShadyWayDbContext _db;
        private readonly int _maxItems;

        public RouteHistoryService(ShadyWayDbContext db, IConfiguration configuration)
        {
            _db       = db;
            _maxItems = configuration.GetValue<int>("History:MaxItems");
        }

        public async Task<List<RouteHistoryItem>> GetHistoryAsync(int userId)
        {
            return await _db.RouteRequests
                .Include(r => r.CalculatedRoute)
                .Where(r => r.CalculatedRoute != null && r.UserId == userId)
                .OrderByDescending(r => r.RequestTime)
                .Take(_maxItems)
                .Select(r => new RouteHistoryItem
                {
                    RequestId           = r.RequestId,
                    SourceAddress       = r.SourceAddress,
                    DestAddress         = r.DestAddress,
                    RequestTime         = r.RequestTime,
                    TotalDistanceMeters = r.CalculatedRoute!.TotalDistance,
                    ShadowPercentage    = r.CalculatedRoute!.ShadowPercentage
                })
                .ToListAsync();
        }
    }
}
