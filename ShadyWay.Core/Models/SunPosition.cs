namespace ShadyWay.Core.Models
{
    // תוצאת חישוב מיקום השמש בשמיים
    public class SunPosition
    {
        // אזימוט – כיוון השמש האופקי (מעלות, 0=צפון, 90=מזרח, 180=דרום, 270=מערב)
        public double Azimuth { get; set; }

        // גובה – זווית השמש מעל האופק (מעלות, 0=אופק, 90=זנית)
        // ערך שלילי = השמש מתחת לאופק (לילה)
        public double Elevation { get; set; }

        // האם יום (השמש מעל האופק)?
        public bool IsDaytime => Elevation > 0;
    }
}
