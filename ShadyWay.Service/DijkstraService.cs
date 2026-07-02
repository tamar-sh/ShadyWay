using ShadyWay.Core.Models;

namespace ShadyWay.Service
{

    public class DijkstraService : IDijkstraService
    {
        public RouteResult FindShadedRoute(
            Dictionary<long, GraphNode> graph,
            long startNodeId,
            long endNodeId,
            double shadowPreference)
        {
            if (!graph.ContainsKey(startNodeId) || !graph.ContainsKey(endNodeId))
                return new RouteResult { Found = false };

            var dist  = new Dictionary<long, double>();
            var prev  = new Dictionary<long, long>();
            var queue = new PriorityQueue<long, double>();

            foreach (var id in graph.Keys)
                dist[id] = double.MaxValue;

            dist[startNodeId] = 0;
            queue.Enqueue(startNodeId, 0);

            bool reachedEnd = false;
            while (queue.Count > 0 && !reachedEnd)
            {
                var current = queue.Dequeue();

                if (current == endNodeId)
                {
                    reachedEnd = true;
                }
                else if (graph.TryGetValue(current, out var node))
                {
                    foreach (var edge in node.Edges)
                    {
                        var cost = dist[current] + edge.GetEffectiveCost(shadowPreference);
                        //שיטת ההקלה...
                        if (cost < dist.GetValueOrDefault(edge.ToNodeId, double.MaxValue))
                        {
                            dist[edge.ToNodeId] = cost;
                            prev[edge.ToNodeId] = current;
                            queue.Enqueue(edge.ToNodeId, cost);
                        }
                    }
                }
            }

            if (dist[endNodeId] == double.MaxValue)
                return new RouteResult { Found = false };

            var path     = ReconstructPath(prev, startNodeId, endNodeId);
            var nodePath = path.Select(id => graph[id]).ToList();

            return BuildResult(nodePath);
        }

        private static List<long> ReconstructPath(
            Dictionary<long, long> prev, long start, long end)
        {
            var path    = new List<long>();
            var current = end;

            while (current != start)
            {
                path.Add(current);
                current = prev[current];
            }

            path.Add(start);
            path.Reverse();
            return path;
        }

        private static RouteResult BuildResult(List<GraphNode> nodePath)
        {
            double totalDistance = 0;
            double totalShadow   = 0;
            int    edgeCount     = 0;
            var    segmentShadows = new List<double>();

            for (int i = 0; i < nodePath.Count - 1; i++)
            {
                var from = nodePath[i];
                var to   = nodePath[i + 1];

                var edge = from.Edges.FirstOrDefault(e => e.ToNodeId == to.Id);
                if (edge != null)
                {
                    totalDistance += edge.DistanceMeters;
                    totalShadow   += edge.ShadowPercentage;
                    edgeCount++;
                    segmentShadows.Add(edge.ShadowPercentage);
                }
                else
                {
                    segmentShadows.Add(0);
                }
            }

            return new RouteResult
            {
                Found                    = true,
                NodePath                 = nodePath,
                SegmentShadowPercentages = segmentShadows,
                TotalDistanceMeters      = totalDistance,
                AverageShadowPercentage  = edgeCount > 0 ? totalShadow / edgeCount : 0
            };
        }
    }
}
