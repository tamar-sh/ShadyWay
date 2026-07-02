using ShadyWay.Core.Models;

namespace ShadyWay.Service
{
    public interface IRouteService
    {
        Task<RouteResult> CalculateRouteAsync(
            double   startLat,        double startLon,
            double   endLat,          double endLon,
            double   shadowPreference,
            DateTime utcDateTime,
            int      userId);
    }
}
