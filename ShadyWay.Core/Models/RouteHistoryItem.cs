namespace ShadyWay.Core.Models
{
    public class RouteHistoryItem
    {
        public int      RequestId           { get; set; }
        public string   SourceAddress       { get; set; } = string.Empty;
        public string   DestAddress         { get; set; } = string.Empty;
        public DateTime RequestTime         { get; set; }
        public float    TotalDistanceMeters { get; set; }
        public float    ShadowPercentage    { get; set; }
    }
}
