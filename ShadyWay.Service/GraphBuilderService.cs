using ShadyWay.Core.Models;
using ShadyWay.Infrastructure.ExternalApis.Osm;

namespace ShadyWay.Service
{
    public class GraphBuilderService : IGraphBuilderService
    {
        private readonly IOsmClient _osmClient;

        // סוגי דרכים המותרים להולכי רגל בלבד
        private static readonly HashSet<string> PedestrianHighways = new(StringComparer.OrdinalIgnoreCase)
        {
            "footway", "pedestrian", "path", "steps",
            "sidewalk", "living_street", "track", "residential", "service",
            "unclassified", "tertiary", "secondary", "primary"
        };

        public GraphBuilderService(IOsmClient osmClient)
        {
            _osmClient = osmClient;
        }

        public async Task<(Dictionary<long, GraphNode> Graph, List<BuildingInfo> Buildings)> BuildGraphAsync(BoundingBox bbox)
        {
            var mapData = await _osmClient.GetOsmMapDataAsync(bbox);
            var graph = BuildGraph(mapData);
            return (graph, mapData.Buildings);
        }

        // ממיר OsmMapData לגרף
        private static Dictionary<long, GraphNode> BuildGraph(OsmMapData mapData)
        {
            var graph = new Dictionary<long, GraphNode>();

            // יוצר GraphNode לכל צומת OSM
            foreach (var (id, (lat, lon)) in mapData.Nodes)
            {
                graph[id] = new GraphNode { Id = id, Latitude = lat, Longitude = lon };
            }

            // עובר על הדרכים ויוצר קצוות בין צמתים עוקבים
            foreach (var way in mapData.Ways)
            {
                // מסנן רק דרכים להולכי רגל
                if (way.Tags.TryGetValue("highway", out var highwayType) &&
                    PedestrianHighways.Contains(highwayType))
                {
                    for (int i = 0; i < way.NodeIds.Count - 1; i++)
                    {
                        var fromId = way.NodeIds[i];
                        var toId   = way.NodeIds[i + 1];

                        if (graph.TryGetValue(fromId, out var fromNode) &&
                            graph.TryGetValue(toId,   out var toNode))
                        {
                            var distance = Haversine(
                                fromNode.Latitude, fromNode.Longitude,
                                toNode.Latitude,   toNode.Longitude);

                            // כיוון קדימה
                            fromNode.Edges.Add(new GraphEdge
                            {
                                ToNodeId         = toId,
                                DistanceMeters   = distance,
                                ShadowPercentage = 0   // יעודכן על ידי שירות הצל בהמשך
                            });

                            // כיוון אחורה (דרכי הליכה הן בדרך כלל דו-כיווניות)
                            toNode.Edges.Add(new GraphEdge
                            {
                                ToNodeId         = fromId,
                                DistanceMeters   = distance,
                                ShadowPercentage = 0
                            });
                        }
                    }
                }
            }

            return graph;
        }

        // (מחזיר את הצומת הקרוב ביותר מבין צמתי הדרכים בלבד (לא צמתי מבנים
        public GraphNode FindNearestNode(Dictionary<long, GraphNode> graph, double lat, double lon)
        {
            return graph.Values
                .Where(n => n.Edges.Count > 0)
                .OrderBy(n => Haversine(n.Latitude, n.Longitude, lat, lon))
                .First();
        }

        // נוסחת Haversine – מחשבת מרחק בין שתי נקודות GPS במטרים
        private static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6_371_000; 
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
    }
}
