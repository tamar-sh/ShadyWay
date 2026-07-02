namespace ShadyWay.Infrastructure.ExternalApis.GoogleEarth
{

    public class TreeInfo
    {
        // קואורדינטות מרכז העץ / שטח העצים
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // גובה הצמרת במטרים – קריטי לחישוב אורך הצל לפי זווית השמש.
        // ברירת מחדל: 5 מטר (עץ בינוני)
        public double HeightMeters { get; set; } = 5.0;

        // רדיוס צמרת העץ במטרים – קובע את רוחב הצל שנוצר
        // ברירת מחדל: 3 מטר
        public double CanopyRadiusMeters { get; set; } = 3.0;
    }
}


