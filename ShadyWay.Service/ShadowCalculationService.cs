using Microsoft.Extensions.Configuration;
using ShadyWay.Core.Models;
using ShadyWay.Infrastructure.ExternalApis.GoogleEarth;
using ShadyWay.Infrastructure.ExternalApis.Weather;

namespace ShadyWay.Service
{

    public class ShadowCalculationService : IShadowCalculationService
    {
        private readonly ISunPositionService _sunPosition;
        private readonly IGoogleEarthClient  _earthClient;
        private readonly IWeatherClient      _weatherClient;

        private readonly double _sampleIntervalMeters;
        private readonly int    _minSamplePoints;
        private readonly double _searchRadiusMeters;
        // פנס לייזר דק יכול להתפספס ופנס רגיל הסיכויים פחותים שיתפספס ולכן לוקחים מרווח ביטחון לשמש
        private readonly double _shadowAngleTolerance;
        private readonly double _minSunElevationDegrees;
        private readonly double _fullShadowPercentage;
        private readonly double _treeShadowPercentage;

        public ShadowCalculationService(
            ISunPositionService sunPosition,
            IGoogleEarthClient  earthClient,
            IWeatherClient      weatherClient,
            IConfiguration      configuration)
        {
            _sunPosition   = sunPosition;
            _earthClient   = earthClient;
            _weatherClient = weatherClient;

            _sampleIntervalMeters   = configuration.GetValue<double>("Shadow:SampleIntervalMeters");
            _minSamplePoints        = configuration.GetValue<int>("Shadow:MinSamplePoints");
            _searchRadiusMeters     = configuration.GetValue<double>("Shadow:SearchRadiusMeters");
            //פנס לייזר דק יכול להתפספס ופנס רגיל הסיכויים פחותים שיתפספס ולכן לוקחים מרווח ביטחון לשמש
            _shadowAngleTolerance   = configuration.GetValue<double>("Shadow:ShadowAngleTolerance");
            _minSunElevationDegrees = configuration.GetValue<double>("Shadow:MinSunElevationDegrees");
            _fullShadowPercentage   = configuration.GetValue<double>("Shadow:FullShadowPercentage");
            _treeShadowPercentage   = configuration.GetValue<double>("Shadow:TreeShadowPercentage");
        }

        public async Task EnrichGraphWithShadowsAsync(
            Dictionary<long, GraphNode> graph,
            BoundingBox                 bbox,
            DateTime                    utcDateTime,
            List<BuildingInfo>          buildings)
        {
            if (!graph.Any()) return;

            //  עצים מ-Google Earth Engine (מבנים מגיעים כבר כפרמטר מ-OSM)
            var trees = (await _earthClient.GetTreesInAreaAsync(
                bbox.MinLat, bbox.MinLon, bbox.MaxLat, bbox.MaxLon)).ToList();

            // חישוב מיקום השמש לפי מרכז ה-BBox
            var sun = _sunPosition.Calculate(bbox.CenterLat, bbox.CenterLon, utcDateTime);

            // שליפת נתוני מזג אוויר
            var weather = await _weatherClient.GetWeatherAsync(bbox.CenterLat, bbox.CenterLon);

            // אם השמש לא בשמיים או שיש עננים, אין צל — כל הקצוות בגרף מקבלים 100% צל
            if (!sun.IsDaytime || weather.CloudPercentage >= 100)
            {
                foreach (var node in graph.Values)
                {
                foreach (var edge in node.Edges)
                    edge.ShadowPercentage = _fullShadowPercentage;
                }
                return;
            }

            // חישוב צל לכל קצה בגרף, בשילוב מקדם העננות
            foreach (var node in graph.Values)
            {
            foreach (var edge in node.Edges)
            {
                if (graph.TryGetValue(edge.ToNodeId, out var toNode))
                {
                    double rawShadow = CalculateSegmentShadow(
                        node, toNode, sun, buildings, trees, edge.DistanceMeters);

                    // צל גיאומטרי ועננות הם הגנות עצמאיות מהשמש — לוקחים את הגבוה מביניהן, לא כופלים
                    edge.ShadowPercentage = Math.Max(rawShadow, weather.CloudPercentage);
                }
            }
            }
        }

        // דוגם נקודות לאורך הקטע (מספר הנקודות מותאם לאורך הקטע) ומחזיר אחוז צל ממוצע
        private double CalculateSegmentShadow(
            GraphNode          fromNode,
            GraphNode          toNode,
            SunPosition        sun,
            List<BuildingInfo> buildings,
            List<TreeInfo>     trees,
            double              segmentDistanceMeters)
        {
            // קטע ארוך יותר מקבל יותר נקודות דגימה
            int samplePoints = Math.Max(_minSamplePoints,
                (int)Math.Ceiling(segmentDistanceMeters / _sampleIntervalMeters) + 1);

            double totalShadow = 0;

            for (int i = 0; i < samplePoints; i++)
            {
                // t נע בין 0 ל-1 — מנקודת ההתחלה לסיום
                double t   = i / (double)(samplePoints - 1);
                double lat = fromNode.Latitude  + t * (toNode.Latitude  - fromNode.Latitude);
                double lon = fromNode.Longitude + t * (toNode.Longitude - fromNode.Longitude);
                            //שולח לחישוב הצל לאותה נקודה מתוך כל הקטע
                totalShadow += CalculatePointShadow(lat, lon, sun, buildings, trees);
            }

            return totalShadow / samplePoints;
        }

        private double CalculatePointShadow(
            double             lat,
            double             lon,
            SunPosition        sun,
            List<BuildingInfo> buildings,
            List<TreeInfo>     trees)
        {
            // הזווית (הגובה) של השמש מעל קו האופק
            double elevation  = Math.Max(sun.Elevation, _minSunElevationDegrees); 
            double sunElevRad = ToRad(elevation); //ממיר את הזווית (הגובה) ממעלות לרדיאנים 
            double shadowDir = NormalizeAngle(sun.Azimuth + 180.0);//כיוון הצל (אזימוט)

            //בדיקת מבנים   
            foreach (var building in buildings)
            {
                // מרחק מהקיר הקרוב ביותר של המבנה, לא ממרכזו 
                var (nearLat, nearLon, dist) = NearestFootprintPoint(lat, lon, building);
                if (dist <= _searchRadiusMeters)
                {
                    //גובה המבנה = אורך הצל / tan(זווית השמש)
                    double shadowLen = building.HeightMeters / Math.Tan(sunElevRad);

                    // כיוון מהמבנה לנקודת הדרך
                    double bearing   = Bearing(nearLat, nearLon, lat, lon);
                    double angleDiff = AngleDifference(bearing, shadowDir);

                    // האם המרחק של הנקודה מהבניין קצר מאורך הצל וגם הם באותו הכיוון?
                    if (dist <= shadowLen && angleDiff <= _shadowAngleTolerance)
                    {
                        return _fullShadowPercentage;
                    }
                }
            }

            //בדיקת עצים   
            foreach (var tree in trees)
            {
                //מרחק בין העץ לאותה נקודת הדרך
                double dist = HaversineMeters(lat, lon, tree.Latitude, tree.Longitude);
                if (dist <= _searchRadiusMeters)
                {
                    // אורך הצל = גובה העץ / tan(זווית השמש)
                    double shadowLen = tree.HeightMeters / Math.Tan(sunElevRad);
                    // כיוון מהעץ לנקודת הדרך
                    double bearing   = Bearing(tree.Latitude, tree.Longitude, lat, lon);
                    double angleDiff = AngleDifference(bearing, shadowDir);

                    // עצים: מוסיפים את רדיוס הצמרת לאורך הצל
                    if (dist <= shadowLen + tree.CanopyRadiusMeters && angleDiff <= _shadowAngleTolerance)
                    {
                        return _treeShadowPercentage;
                    }
                }
            }

            return 0.0; // אין צל
        }

        // מאתר את הנקודה הקרובה ביותר במתאר המבנה לנקודת הדרך (קירוב לקיר הקרוב ביותר)
        private static (double Lat, double Lon, double Dist) NearestFootprintPoint(
            double lat, double lon, BuildingInfo building)
        {
            if (building.Footprint.Count == 0)
                return (building.Latitude, building.Longitude,
                        HaversineMeters(lat, lon, building.Latitude, building.Longitude));

            double bestDist = double.MaxValue;
            double bestLat  = building.Latitude;
            double bestLon  = building.Longitude;
            //עובר על כל הנקודות של המבנה ומוצא את הנקודה עם המרחק הקטן ביותר לאותו צומת
            foreach (var (fLat, fLon) in building.Footprint)
            {
                double d = HaversineMeters(lat, lon, fLat, fLon);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestLat  = fLat;
                    bestLon  = fLon;
                }
            }

            return (bestLat, bestLon, bestDist);
        }

        // מרחק Haversine בין שתי נקודות GPS במטרים
        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6_371_000;
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        // כיוון (bearing) מנקודה 1 לנקודה 2 0=צפון, 90=מזרח...
        private static double Bearing(double lat1, double lon1, double lat2, double lon2)
        {
            double dLon  = ToRad(lon2 - lon1);
            double lat1r = ToRad(lat1);
            double lat2r = ToRad(lat2);
            double x = Math.Sin(dLon) * Math.Cos(lat2r);
            double y = Math.Cos(lat1r) * Math.Sin(lat2r)
                     - Math.Sin(lat1r) * Math.Cos(lat2r) * Math.Cos(dLon);
            return NormalizeAngle(ToDeg(Math.Atan2(x, y)));
        }

        // הפרש זוויתי מינימלי בין שתי זוויות 
        private static double AngleDifference(double a, double b)
        {
            double diff = Math.Abs(a - b) % 360.0;
            return diff > 180.0 ? 360.0 - diff : diff;
        }

        private static double NormalizeAngle(double angle)
        {
            angle %= 360.0;
            return angle < 0 ? angle + 360.0 : angle;
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
        private static double ToDeg(double rad) => rad * 180.0 / Math.PI;
    }
}
