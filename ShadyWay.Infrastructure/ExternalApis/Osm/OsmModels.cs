using ShadyWay.Core.Models;

namespace ShadyWay.Infrastructure.ExternalApis.Osm
{
    // מכיל את כל נתוני המפה שהורדו מ-OSM: צמתים, דרכים ומבנים
    public class OsmMapData
    {
        public Dictionary<long, (double Lat, double Lon)> Nodes { get; set; } = new();
        public List<OsmWayData> Ways { get; set; } = new();
        public List<BuildingInfo> Buildings { get; set; } = new();
    }

    // מייצג דרך גולמית מ-OSM: רשימת IDs של צמתים + תגים
    public class OsmWayData
    {
        public long Id { get; set; }
        public List<long> NodeIds { get; set; } = new();
        public Dictionary<string, string> Tags { get; set; } = new();
    }

}

