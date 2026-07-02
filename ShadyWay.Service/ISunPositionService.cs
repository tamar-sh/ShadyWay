using ShadyWay.Core.Models;

namespace ShadyWay.Service
{
    public interface ISunPositionService
    {
        // מחשב את מיקום השמש לפי מיקום גיאוגרפי ושעה (UTC)
        SunPosition Calculate(double latitude, double longitude, DateTime utcDateTime);
    }
}
