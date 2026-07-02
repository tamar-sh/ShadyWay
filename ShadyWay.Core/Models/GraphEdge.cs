namespace ShadyWay.Core.Models
{
    public class GraphEdge
    {
        public long ToNodeId { get; set; }
        public double DistanceMeters { get; set; }

        public double ShadowPercentage { get; set; } = 0;// אחוז הצל על הקטע הזה (0-100)
        // עלות אפקטיבית של הקטע לפי העדפת הצל של המשתמש.
        public double GetEffectiveCost(double shadowPreference)
        {
            //                                    העדפת צל של המשתמש           כמה חשיפה לשמש      
            return DistanceMeters * (1 + (1 - ShadowPercentage / 100.0) * (shadowPreference - 1));
        }
    }
}
