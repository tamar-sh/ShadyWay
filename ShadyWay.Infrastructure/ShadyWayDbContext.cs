using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ShadyWay.Core.Models;

namespace ShadyWay.Infrastructure
{
    // ShadyWayDbContext הוא "הגשר" בין הקוד שלנו לבין מסד הנתונים PostgreSQL.
    // הוא יורש מ-DbContext של EF Core ומגדיר אילו טבלאות (DbSet) קיימות.
    public class ShadyWayDbContext : DbContext
    {
        // הבנאי מקבל את ה-DbContextOptions (כגון מחרוזת חיבור) מ-Dependency Injection
        public ShadyWayDbContext(DbContextOptions<ShadyWayDbContext> options) : base(options) { }

        //המחלקות יהפכו לטבלאות בבסיס הנתונים
        public DbSet<User> Users { get; set; }

        public DbSet<RouteRequest> RouteRequests { get; set; }

        public DbSet<CalculatedRoute> CalculatedRoutes { get; set; }

        // OnModelCreating נקרא פעם אחת בהפעלת האפליקציה.
        // כאן מוגדרות ה-Mappings המפורטות בין ה-Models לבין הטבלאות.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- הגדרות לטבלת users ---
            modelBuilder.Entity<User>(entity =>
            {
                // שם הטבלה במסד הנתונים
                entity.ToTable("users");

                // שם העמודה של המפתח הראשי
                entity.HasKey(u => u.UserId);
                entity.Property(u => u.UserId).HasColumnName("user_id");

                entity.Property(u => u.IdentityCard)
                    .HasColumnName("identity_card")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(u => u.FullName)
                    .HasColumnName("full_name")
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(u => u.Email)
                    .HasColumnName("email")
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(u => u.PasswordHash)
                    .HasColumnName("password_hash")
                    .IsRequired();

                // ערך ברירת מחדל 1.2 – כמות ברירת המחדל של הארכת הדרך לטובת צל
                entity.Property(u => u.ShadowPreference)
                    .HasColumnName("shadow_preference")
                    .HasDefaultValue(1.2f);

                entity.Property(u => u.FailedLoginAttempts)
                    .HasColumnName("failed_login_attempts")
                    .HasDefaultValue(0);

                entity.Property(u => u.LockedUntil)
                    .HasColumnName("locked_until");

                // אינדקסים ייחודיים (UNIQUE) התואמים את ההגדרות במסד הנתונים
                entity.HasIndex(u => u.IdentityCard).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();

                // קשר 1-to-Many: משתמש אחד יכול לשלוח מספר בקשות ניווט
                entity.HasMany(u => u.RouteRequests)
                    .WithOne(r => r.User)
                    .HasForeignKey(r => r.UserId);
            });

            // --- הגדרות לטבלת route_requests ---
            modelBuilder.Entity<RouteRequest>(entity =>
            {
                entity.ToTable("route_requests");

                entity.HasKey(r => r.RequestId);
                entity.Property(r => r.RequestId).HasColumnName("request_id");

                entity.Property(r => r.UserId).HasColumnName("user_id");

                // זמן הבקשה נוצר אוטומטית בצד מסד הנתונים (CURRENT_TIMESTAMP)
                entity.Property(r => r.RequestTime)
                    .HasColumnName("request_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(r => r.SourceAddress).HasColumnName("source_address");
                entity.Property(r => r.DestAddress).HasColumnName("dest_address");

                // קשר 1-to-1: בקשה אחת יכולה לקבל מסלול מחושב אחד
                entity.HasOne(r => r.CalculatedRoute)
                    .WithOne(c => c.RouteRequest)
                    .HasForeignKey<CalculatedRoute>(c => c.RequestId);
            });

            // --- הגדרות לטבלת calculated_routes ---
            modelBuilder.Entity<CalculatedRoute>(entity =>
            {
                entity.ToTable("calculated_routes");

                entity.HasKey(c => c.RouteId);
                entity.Property(c => c.RouteId).HasColumnName("route_id");

                entity.Property(c => c.RequestId).HasColumnName("request_id");
                entity.Property(c => c.TotalDistance).HasColumnName("total_distance");
                entity.Property(c => c.EstimatedTime).HasColumnName("estimated_time");
                entity.Property(c => c.ShadowPercentage).HasColumnName("shadow_percentage");

                // שדה הגיאומטריה: LineString עם SRID 4326 (WGS84 – קואורדינטות GPS סטנדרטיות).
                // HasColumnType מגדיר לEF Core שזהו שדה PostGIS גיאוגרפי.
                entity.Property(c => c.RouteGeometry)
                    .HasColumnName("route_geometry")
                    .HasColumnType("geometry(LineString, 4326)");
            });
        }
    }
}
