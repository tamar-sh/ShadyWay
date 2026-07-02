namespace ShadyWay.Infrastructure.ExternalApis.Weather
{
    public interface IWeatherClient
    {
        // מחזיר נתוני מזג אוויר עדכניים לפי מיקום גיאוגרפי
        Task<WeatherInfo> GetWeatherAsync(double latitude, double longitude);
    }
}
