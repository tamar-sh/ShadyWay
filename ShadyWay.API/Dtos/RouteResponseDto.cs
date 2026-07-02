namespace ShadyWay.API.Dtos
{
    public class RouteResponseDto
    {
        public bool Found { get; set; }
        public double TotalDistanceMeters { get; set; }
        public double EstimatedWalkingTimeMinutes { get; set; }
        public double AverageShadowPercentage { get; set; }

        // רשימת נקודות הגיאוגרפיות של המסלול (lat, lon) לציור על המפה
        public List<CoordinateDto> Path { get; set; } = new();

        // אחוז צל לכל קטע בין שתי נקודות עוקבות ב-Path — אורכה קטן ב-1 מ-Path
        public List<double> SegmentShadowPercentages { get; set; } = new();
    }

    public class CoordinateDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}



