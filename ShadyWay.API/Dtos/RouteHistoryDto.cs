namespace ShadyWay.API.Dtos
{
    public class RouteHistoryDto
    {
        public int      RequestId           { get; set; }
        public string   SourceAddress       { get; set; } = "";
        public string   DestAddress         { get; set; } = "";
        public DateTime RequestTime         { get; set; }
        public float    TotalDistanceMeters { get; set; }
        public float    ShadowPercentage    { get; set; }
    }
}
