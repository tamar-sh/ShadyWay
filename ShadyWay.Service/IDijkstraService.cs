using ShadyWay.Core.Models;

namespace ShadyWay.Service
{
    public interface IDijkstraService
    {
        RouteResult FindShadedRoute(
            Dictionary<long, GraphNode> graph,
            long startNodeId,
            long endNodeId,
            double shadowPreference);
    }
}
