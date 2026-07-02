using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShadyWay.Infrastructure;
using ShadyWay.Infrastructure.ExternalApis.GoogleEarth;
using ShadyWay.Infrastructure.ExternalApis.Osm;
using ShadyWay.Infrastructure.ExternalApis.Weather;
using ShadyWay.Service;

namespace ShadyWay.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // --- רישום ה-DbContext ב-Dependency Injection ---
            // קורא את מחרוזת החיבור מ-appsettings.json
            // UseNpgsql מגדיר שאנחנו עובדים עם PostgreSQL
            // UseNetTopologySuite מאפשר תמיכה בשדות גיאומטריה (PostGIS)
            builder.Services.AddDbContext<ShadyWayDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.UseNetTopologySuite()
                )
            );

            // --- רישום לקוחות ה-API החיצוניים ב-Dependency Injection ---
            // AddHttpClient<Interface, Implementation> יוצר HttpClient ייעודי לכל לקוח.
            // כל לקוח מקבל HttpClient מנוהל (connection pooling, timeout וכו')
            // ולא HttpClient ידני שעלול לגרום לבעיות Socket exhaustion.

            // לקוח Google Earth Engine – לשליפת מבנים וגבהים לחישוב צל
            builder.Services.AddHttpClient<IGoogleEarthClient, GoogleEarthClient>();

            // לקוח OpenStreetMap (Overpass API) – לשליפת דרכים, מבנים ועצים
            builder.Services.AddHttpClient<IOsmClient, OsmClient>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ShadyWay/1.0");
                client.Timeout = TimeSpan.FromSeconds(90);
            });

            // לקוח OpenWeatherMap – לשליפת נתוני עננות ומזג אוויר
            builder.Services.AddHttpClient<IWeatherClient, WeatherClient>();

            // --- CORS: מאפשר לאפליקציית React לתקשר עם ה-API ---
            // WithOrigins מגדיר את כתובות ה-React המותרות (Vite=5173, CRA=3000)
            builder.Services.AddCors(options =>
                options.AddPolicy("ReactClient", policy =>
                    policy.WithOrigins(
                              "https://localhost:5173",
                              "http://localhost:5173",
                              "https://localhost:5174",
                              "http://localhost:5174",
                              "https://localhost:3000",
                              "http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                )
            );

            // --- רישום שירותי ה-Service ב-Dependency Injection ---
            // GraphBuilderService: בונה גרף ניתוב מנתוני OSM
            builder.Services.AddScoped<IGraphBuilderService, GraphBuilderService>();

            // DijkstraService: מוצא מסלול מוטה-צל בגרף
            builder.Services.AddScoped<IDijkstraService, DijkstraService>();

            // SunPositionService: מחשב מיקום השמש לפי Meeus (אזימוט + גובה)
            builder.Services.AddScoped<ISunPositionService, SunPositionService>();

            // ShadowCalculationService: מחשב אחוז צל לכל קצה בגרף
            builder.Services.AddScoped<IShadowCalculationService, ShadowCalculationService>();

            // RouteService: מרכז את לוגיקת חישוב המסלול (בנייה + Dijkstra)
            builder.Services.AddScoped<IRouteService, RouteService>();

            // RouteHistoryService: שליפת היסטוריית מסלולים של משתמש
            builder.Services.AddScoped<IRouteHistoryService, RouteHistoryService>();

            // --- הגדרת JWT Authentication ---
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidateAudience         = true,
                        ValidateLifetime         = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
                        ValidAudience            = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey         = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };
                });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("ReactClient");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
