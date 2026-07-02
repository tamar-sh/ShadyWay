using ShadyWay.Core.Models;

namespace ShadyWay.Service
{
    public interface IShadowCalculationService
    {
        // מחשב אחוז צל לכל קצה בגרף ומעדכן את ShadowPercentage בכל Edge
        // buildings מגיע מ-OSM (כבר חולץ בעת בניית הגרף)
        Task EnrichGraphWithShadowsAsync(
            Dictionary<long, GraphNode> graph,
            BoundingBox                 bbox,
            DateTime                    utcDateTime,
            List<BuildingInfo>          buildings);
    }
}
