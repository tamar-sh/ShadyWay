namespace ShadyWay.Core.Models
{
    public class GraphNode
    {
        public long Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<GraphEdge> Edges { get; set; } = new List<GraphEdge>();
    }
}
