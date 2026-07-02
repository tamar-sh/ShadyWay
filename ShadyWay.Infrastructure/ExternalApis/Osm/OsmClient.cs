using Microsoft.Extensions.Configuration;
using OsmSharp.Streams;
using ShadyWay.Core.Models;

namespace ShadyWay.Infrastructure.ExternalApis.Osm
{
    public class OsmClient : IOsmClient
    {
        private readonly HttpClient _httpClient;
        private readonly string[]   _overpassMirrors;
        private readonly int        _overpassQueryTimeoutSeconds;
        private readonly double     _metersPerBuildingLevel;
        private readonly double     _defaultBuildingHeightMeters;

        public OsmClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient                  = httpClient;
            _overpassMirrors             = configuration.GetSection("Osm:OverpassMirrors").Get<string[]>() ?? [];
            _overpassQueryTimeoutSeconds = configuration.GetValue<int>("Osm:OverpassQueryTimeoutSeconds");
            _metersPerBuildingLevel      = configuration.GetValue<double>("Osm:MetersPerBuildingLevel");
            _defaultBuildingHeightMeters = configuration.GetValue<double>("Osm:DefaultBuildingHeightMeters");
        }

        public async Task<OsmMapData> GetOsmMapDataAsync(BoundingBox bbox)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var b = $"{bbox.MinLat.ToString(inv)},{bbox.MinLon.ToString(inv)},{bbox.MaxLat.ToString(inv)},{bbox.MaxLon.ToString(inv)}";
            var query = $"[out:xml][timeout:{_overpassQueryTimeoutSeconds}];(way[\"highway\"~\"footway|pedestrian|path|steps|sidewalk|living_street|track|residential|service|unclassified|tertiary|secondary|primary\"]({b});way[\"building\"]({b}););out body;>;out skel qt;";

            Stream? stream = null;
            var errors = new List<string>();//בשביל הבדיקה איזה שרתים לא הצליח
            for (int i = 0; i < _overpassMirrors.Length && stream == null; i++)
            {
                var mirror = _overpassMirrors[i];
                try
                {
                    var content = new FormUrlEncodedContent(
                        new[] { new KeyValuePair<string, string>("data", query) });
                    var response = await _httpClient.PostAsync(mirror, content);
                    response.EnsureSuccessStatusCode();
                    stream = await response.Content.ReadAsStreamAsync();
                }
                catch (Exception ex)
                {
                    errors.Add($"{mirror}: {ex.GetType().Name}: {ex.Message}");
                }
            }
            if (stream == null)
                throw new HttpRequestException($"כל שרתי Overpass נכשלו.\n{string.Join("\n", errors)}");

            var data = new OsmMapData();
            using var source = new XmlOsmStreamSource(stream);//אפשרות לקריאה 
            foreach (var element in source)//קריאה אחד אחד שלא כולם יהיו על הזיכרון באותו זמן מה שתופס מקום 
            {
                if (element is OsmSharp.Node node && node.Id.HasValue &&
                    node.Latitude.HasValue && node.Longitude.HasValue)
                {
                    data.Nodes[node.Id.Value] = (node.Latitude.Value, node.Longitude.Value);
                }
                else if (element is OsmSharp.Way way && way.Id.HasValue && way.Nodes != null)
                {
                    var tags = way.Tags?.ToDictionary(t => t.Key, t => t.Value)
                               ?? new Dictionary<string, string>();

                    data.Ways.Add(new OsmWayData
                    {
                        Id      = way.Id.Value,
                        NodeIds = way.Nodes.ToList(),
                        Tags    = tags
                    });
                }
            }

            foreach (var way in data.Ways.Where(w => w.Tags.ContainsKey("building")))
            {
                var coords = way.NodeIds
                    //מסנן לי רק את הצמתים הרלוונטיים שיש להם קוארדינטות זאת אומרת שהם היו מיוצגים כיחידה בקובץ
                    .Where(data.Nodes.ContainsKey)
                    .Select(nId => data.Nodes[nId])
                    .ToList();

                if (coords.Count >= 3)
                {
                    double height = ParseBuildingHeight(way.Tags);

                    data.Buildings.Add(new BuildingInfo
                    {
                        Latitude     = coords.Average(c => c.Lat),
                        Longitude    = coords.Average(c => c.Lon),
                        HeightMeters = height,
                        Footprint    = coords.Select(c => (c.Lat, c.Lon)).ToList()
                    });
                }
            }

            return data;
        }

        private double ParseBuildingHeight(Dictionary<string, string> tags)
        {
            if (tags.TryGetValue("height", out var heightStr))
            {
                var token = heightStr.Trim().Split(' ')[0];
                if (double.TryParse(token, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var h) && h > 0)
                    return h;
            }
            if (tags.TryGetValue("building:levels", out var levelsStr) &&
                double.TryParse(levelsStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var levels) && levels > 0)
                return levels * _metersPerBuildingLevel;

            return _defaultBuildingHeightMeters;
        }
    }
}
