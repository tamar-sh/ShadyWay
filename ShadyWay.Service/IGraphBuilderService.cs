using ShadyWay.Core.Models;

namespace ShadyWay.Service
{
    public interface IGraphBuilderService
    {
        // מחזיר את הגרף ואת רשימת המבנים שחולצה מאותו XML
        Task<(Dictionary<long, GraphNode> Graph, List<BuildingInfo> Buildings)> BuildGraphAsync(BoundingBox bbox);

        // מחזיר את הצומת הקרוב ביותר בגרף לנקודה הנתונה
        GraphNode FindNearestNode(Dictionary<long, GraphNode> graph, double lat, double lon);
    }
}
