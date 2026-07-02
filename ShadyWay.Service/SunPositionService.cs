using ShadyWay.Core.Models;

namespace ShadyWay.Service
{
    // מחשב את מיקום השמש בשמיים לפי אלגוריתם Meeus 
    // מחזיר אזימוט (כיוון אופקי) וגובה (זווית מעל האופק) לכל מיקום ושעה.
    public class SunPositionService : ISunPositionService
    {
        public SunPosition Calculate(double latitude, double longitude, DateTime utcDateTime)
        {
            //השורות האלו נועדו להגדיר את "נקודת הזמן" שביחס אליה אנחנו מודדים את תנועת השמש
            //(
            // שלב 1: המרת תאריך ושעה לתאריך יוליאני (Julian Day)
            double jd = ToJulianDay(utcDateTime);
            // שלב 2: חישוב מאה השנים מאז 1 בינואר 2000 (הנקודה שבה השמש הייתה במיקום "0" על המסלול שלה)
            double jc = (jd - 2451545.0) / 36525.0;
            //)

            // שלב 3: אורך ממוצע של השמש (מעלות)
            //לפי הזמן שעבר, השמש אמורה להיות בנקודה מספר איקס על המעגל
            double L0 = 280.46646 + jc * (36000.76983 + jc * 0.0003032);
            L0 = NormalizeAngle(L0);

            // שלב 4: אנומליה ממוצעת של השמש (מעלות)  
            //מד מהירות הסיבוב של כדור הארץ סביב השמש, שמראה איפה השמש אמורה להיות על המסלול שלה  
            double M    = 357.52911 + jc * (35999.05029 - jc * 0.0001537);
            M           = NormalizeAngle(M);
            double Mrad = ToRad(M);

            // שלב 5: משוואת המרכז — תיקון לאליפסה של מסלול כדור הארץ
            double C = (1.914602 - jc * (0.004817 + 0.000014 * jc)) * Math.Sin(Mrad)
                     + (0.019993 - 0.000101 * jc)                    * Math.Sin(2 * Mrad)
                     +  0.000289                                       * Math.Sin(3 * Mrad);

            // שלב 6: אורך אמיתי של השמש
            double sunLon = L0 + C;

            // שלב 7: אורך נראה — תיקון לאברציה (סטייה קטנה בגלל מהירות האור)
            //תיקון הסטייה בגלל מהירות האור ונדנוד כדור הארץ
            double omega  = 125.04 - 1934.136 * jc;
            double lambda = sunLon - 0.00569 - 0.00478 * Math.Sin(ToRad(omega));

            // שלב 8: נטיית מישור המילקה (זווית בין מישור קו המשווה למישור המסלול)
            double epsilon0 = 23.0
                + (26.0
                + (21.448 - jc * (46.8150 + jc * (0.00059 - jc * 0.001813)))
                / 60.0) / 60.0;
            double epsilon    = epsilon0 + 0.00256 * Math.Cos(ToRad(omega));
            double epsilonRad = ToRad(epsilon);
            double lambdaRad  = ToRad(lambda);

            // שלב 9: עליה ישרה ונטייה — קואורדינטות השמש במערכת המשווינית
            double RA   = Math.Atan2(Math.Cos(epsilonRad) * Math.Sin(lambdaRad),
                                     Math.Cos(lambdaRad));
            RA          = NormalizeAngle(ToDeg(RA)) / 15.0; // המרה לשעות (360°=24h)
            double decl = ToDeg(Math.Asin(Math.Sin(epsilonRad) * Math.Sin(lambdaRad)));

            // שלב 10: זמן כוכבי גריניץ' — "שעון" אסטרונומי
            double GMST = 280.46061837
                        + 360.98564736629 * (jd - 2451545.0)
                        + jc * jc * (0.000387933 - jc / 38710000.0);
            GMST = NormalizeAngle(GMST);

            // שלב 11: זווית שעה מקומית — כמה שעות עברו מאז השמש "עברה" מעל המיקום שלנו
            double LHA    = NormalizeAngle(GMST + longitude - RA * 15.0);
            double LHArad = ToRad(LHA);
            double latRad = ToRad(latitude);
            double declRad = ToRad(decl);

            // שלב 12: גובה השמש מעל האופק (elevation)
            double elevation = ToDeg(Math.Asin(
                Math.Sin(latRad)  * Math.Sin(declRad) +
                Math.Cos(latRad)  * Math.Cos(declRad) * Math.Cos(LHArad)));

            // שלב 13: תיקון שבירת אטמוספרה — האטמוספרה "מכופפת" את קרני האור,
            // ולכן השמש נראית גבוהה מעט יותר ממה שהיא באמת
            elevation += AtmosphericRefraction(elevation);

            // שלב 14: אזימוט — כיוון השמש (0=צפון, 90=מזרח, 180=דרום, 270=מערב)
            double azimuth = ToDeg(Math.Atan2(
                Math.Sin(LHArad),
                Math.Cos(LHArad) * Math.Sin(latRad) - Math.Tan(declRad) * Math.Cos(latRad)));
            azimuth = NormalizeAngle(azimuth + 180.0);

            return new SunPosition
            {
                Azimuth   = Math.Round(azimuth,   4),
                Elevation = Math.Round(elevation, 4)
            };
        }

        // מחשב תיקון שבירת אטמוספרה בארקמינוטות (1/60 מעלה)
        private static double AtmosphericRefraction(double elevation)
        {
            if (elevation > 85)
                return 0;

            double refraction;
            if (elevation > 5)
                refraction = 58.1  / Math.Tan(ToRad(elevation))
                           - 0.07  / Math.Pow(Math.Tan(ToRad(elevation)), 3)
                           + 0.000086 / Math.Pow(Math.Tan(ToRad(elevation)), 5);
            else if (elevation > -0.575)
                refraction = 1735 + elevation
                           * (-518.2 + elevation
                           * ( 103.4 + elevation
                           * ( -12.79 + elevation * 0.711)));
            else
                refraction = -20.772 / Math.Tan(ToRad(elevation));

            return refraction / 3600.0; // המרה ממינוטות קשת למעלות
        }

        // המרת DateTime לתאריך יוליאני
        private static double ToJulianDay(DateTime dt)
        {
            int    Y = dt.Year;
            int    M = dt.Month;
            double D = dt.Day
                     + dt.Hour   / 24.0
                     + dt.Minute / 1440.0
                     + dt.Second / 86400.0;

            if (M <= 2) { Y--; M += 12; }

            int A = Y / 100;
            int B = 2 - A + A / 4;

            return Math.Floor(365.25  * (Y + 4716))
                 + Math.Floor(30.6001 * (M + 1))
                 + D + B - 1524.5;
        }

        // נרמול זווית לטווח 0-360
        private static double NormalizeAngle(double angle)
        {
            angle %= 360.0;
            return angle < 0 ? angle + 360.0 : angle;
        }

        private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
        private static double ToDeg(double radians) => radians * 180.0 / Math.PI;
    }
}
