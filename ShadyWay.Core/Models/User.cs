namespace ShadyWay.Core.Models
{
    // מייצג משתמש רשום במערכת.
    // מתאים לטבלת users במסד הנתונים.
    public class User
    {
        // מפתח ראשי – נוצר אוטומטית על ידי PostgreSQL (SERIAL)
        public int UserId { get; set; }
        public string IdentityCard { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;

        public float ShadowPreference { get; set; } = 1.2f;

        // מונה ניסיונות התחברות כושלים רצופים — מתאפס בהתחברות מוצלחת
        public int FailedLoginAttempts { get; set; } = 0;

        // אם קיים ועדיין בעתיד — החשבון חסום זמנית מהתחברות
        public DateTime? LockedUntil { get; set; }

        // רשימת כל בקשות הניווט שהמשתמש ביצע
        public ICollection<RouteRequest> RouteRequests { get; set; } = new List<RouteRequest>();
    }
}
