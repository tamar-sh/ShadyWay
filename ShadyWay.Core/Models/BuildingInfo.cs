namespace ShadyWay.Core.Models
{
    public class BuildingInfo
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public double HeightMeters { get; set; } = 10.0;

        public List<(double Lat, double Lon)> Footprint { get; set; } = new();
    }
}
