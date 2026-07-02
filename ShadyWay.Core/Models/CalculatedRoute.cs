using NetTopologySuite.Geometries;

namespace ShadyWay.Core.Models
{
    // מייצג מסלול הליכה מוצל שחושב עבור בקשת ניווט.
    public class CalculatedRoute
    {
        // מפתח ראשי – נוצר אוטומטית על ידי PostgreSQL (SERIAL)
        public int RouteId { get; set; }

        // מפתח זר המקשר את המסלול לבקשה שיצרה אותו
        public int RequestId { get; set; }

        public float TotalDistance { get; set; }
        public int EstimatedTime { get; set; }// זמן הליכה משוער, נמדד בדקות
        public float ShadowPercentage { get; set; }
        public LineString RouteGeometry { get; set; } = null!;

        // Navigation property: הבקשה המקורית שעבורה חושב המסלול
        public RouteRequest RouteRequest { get; set; } = null!;
    }
}
