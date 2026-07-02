using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using ShadyWay.Core.Models;
using ShadyWay.Infrastructure;

namespace ShadyWay.Service
{
    public class RouteService : IRouteService
    {
        private readonly IGraphBuilderService      _graphBuilder;
        private readonly IShadowCalculationService _shadowCalc;
        private readonly IDijkstraService          _dijkstra;
        private readonly ShadyWayDbContext         _db;

        private readonly double _paddingPercent;
        private readonly double _minLatPad;
        private readonly double _minLonPad;

        private static readonly GeometryFactory GeoFactory =
            new GeometryFactory(new PrecisionModel(), 4326);

        public RouteService(
            IGraphBuilderService      graphBuilder,
            IShadowCalculationService shadowCalc,
            IDijkstraService          dijkstra,
            ShadyWayDbContext         db,
            IConfiguration            configuration)
        {
            _graphBuilder = graphBuilder;
            _shadowCalc   = shadowCalc;
            _dijkstra     = dijkstra;
            _db           = db;

            _paddingPercent = configuration.GetValue<double>("Route:BBoxPaddingPercent");
            _minLatPad      = configuration.GetValue<double>("Route:MinLatPaddingDegrees");
            _minLonPad      = configuration.GetValue<double>("Route:MinLonPaddingDegrees");
        }

        public async Task<RouteResult> CalculateRouteAsync(
            double   startLat,        double startLon,
            double   endLat,          double endLon,
            double   shadowPreference,
            DateTime utcDateTime,
            int      userId)
        {
            // חישוב BBox עם ריפוד + מינימום מוחלט כדי שהגרף לא יהיה צר מדי
            double minLat = Math.Min(startLat, endLat);//הנקודה הדרומית ביותר
            double maxLat = Math.Max(startLat, endLat);//הנקודה הצפונית ביותר
            double minLon = Math.Min(startLon, endLon);//הנקודה המערבית ביותר
            double maxLon = Math.Max(startLon, endLon);//הנקודה המזרחית ביותר
            double latPad = Math.Max((maxLat - minLat) * _paddingPercent, _minLatPad);
            double lonPad = Math.Max((maxLon - minLon) * _paddingPercent, _minLonPad);
            var bbox = new BoundingBox
            {
                MinLat = minLat - latPad,
                MaxLat = maxLat + latPad,
                MinLon = minLon - lonPad,
                MaxLon = maxLon + lonPad
            };

            // שלב 1: בניית גרף + חילוץ מבנים מנתוני OSM 
            var (graph, buildings) = await _graphBuilder.BuildGraphAsync(bbox);

            if (graph.Count == 0)
                return new RouteResult { Found = false };

            // שלב 2: חישוב צל לכל קצה — מבנים מ-OSM, עצים מ-GEE
            await _shadowCalc.EnrichGraphWithShadowsAsync(graph, bbox, utcDateTime, buildings);

            // שלב 3: מציאת הצמתים הקרובים לנקודות ההתחלה והסיום
            var startNode = _graphBuilder.FindNearestNode(graph, startLat, startLon);
            var endNode   = _graphBuilder.FindNearestNode(graph, endLat,   endLon);

            // שלב 4: הרצת דייקסטרה עם משקל צל
            var result = _dijkstra.FindShadedRoute(
                graph, startNode.Id, endNode.Id, shadowPreference);

            // שלב 5: שמירת הבקשה והמסלול למסד הנתונים
            if (result.Found)
                await SaveRouteAsync(result, startLat, startLon, endLat, endLon, userId);

            return result;
        }

        private async Task SaveRouteAsync(
            RouteResult result,
            double startLat, double startLon,
            double endLat,   double endLon,
            int    userId)
        {
            var routeRequest = new RouteRequest
            {
                UserId        = userId,
                RequestTime   = DateTime.UtcNow,
                SourceAddress = $"{startLat:F6},{startLon:F6}",
                DestAddress   = $"{endLat:F6},{endLon:F6}"
            };
            _db.RouteRequests.Add(routeRequest);
            await _db.SaveChangesAsync();

            // בניית LineString מרשימת הצמתים
            var coordinates = result.NodePath
                .Select(n => new Coordinate(n.Longitude, n.Latitude))
                .ToArray();
            var lineString = GeoFactory.CreateLineString(coordinates);

            // שמירת המסלול המחושב
            var calculatedRoute = new CalculatedRoute
            {
                RequestId       = routeRequest.RequestId,
                TotalDistance   = (float)result.TotalDistanceMeters,
                EstimatedTime   = (int)Math.Round(result.EstimatedWalkingTimeMinutes),
                ShadowPercentage = (float)result.AverageShadowPercentage,
                RouteGeometry   = lineString
            };
            _db.CalculatedRoutes.Add(calculatedRoute);
            await _db.SaveChangesAsync();
        }
    }
}
