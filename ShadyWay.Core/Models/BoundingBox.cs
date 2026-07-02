namespace ShadyWay.Core.Models
{
    public class BoundingBox
    {
        public double MinLat { get; set; }
        public double MinLon { get; set; }
        public double MaxLat { get; set; }
        public double MaxLon { get; set; }

        //לחישוב זווית השמש מחשבים את האמצע של המסלול
        public double CenterLat => (MinLat + MaxLat) / 2;
        public double CenterLon => (MinLon + MaxLon) / 2;

        // פורמט Overpass /api/map: minLon,minLat,maxLon,maxLat
        public string ToOsmString() => $"{MinLon},{MinLat},{MaxLon},{MaxLat}";
    }
}
