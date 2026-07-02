namespace ShadyWay.Core.Models
{
    // מייצג בקשת ניווט שמשתמש שלח למערכת.
    // מתאים לטבלת route_requests במסד הנתונים.
    public class RouteRequest
    {
        // מפתח ראשי – נוצר אוטומטית על ידי PostgreSQL (SERIAL)
        public int RequestId { get; set; }

        // מפתח זר המקשר את הבקשה למשתמש שביצע אותה
        public int UserId { get; set; }

        public DateTime RequestTime { get; set; }
        public string SourceAddress { get; set; } = null!;
        public string DestAddress { get; set; } = null!;

        // Navigation property: המשתמש שביצע את הבקשה
        public User User { get; set; } = null!;

        // Navigation property: המסלול המחושב שהתקבל עבור בקשה זו (יכול להיות null אם עדיין לא חושב)
        public CalculatedRoute? CalculatedRoute { get; set; }
    }
}
