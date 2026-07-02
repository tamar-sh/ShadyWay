namespace ShadyWay.Core.Models
{
    public class RouteResult
    {
        public bool Found { get; set; }

        // רשימת הצמתים בסדר מהמקור ליעד
        public List<GraphNode> NodePath { get; set; } = new List<GraphNode>();

        // אחוז הצל של כל קטע בין שני צמתים עוקבים — אורכה קטן ב-1 מ-NodePath
        public List<double> SegmentShadowPercentages { get; set; } = new List<double>();

        public double TotalDistanceMeters { get; set; }
        public double AverageShadowPercentage { get; set; }

        // זמן הליכה משוער
        public double EstimatedWalkingTimeMinutes => TotalDistanceMeters / 80.0;
    }
}
