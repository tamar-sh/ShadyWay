using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ShadyWay.Infrastructure.ExternalApis.Weather
{
    public class WeatherClient : IWeatherClient
    {
        private readonly HttpClient _httpClient;
        private readonly string     _apiKey;
        private readonly string     _baseUrl;

        public WeatherClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey     = configuration["Weather:OpenWeatherMapApiKey"]!;
            _baseUrl    = configuration["Weather:BaseUrl"]!;
        }

        public async Task<WeatherInfo> GetWeatherAsync(double latitude, double longitude)
        {
            var url = $"{_baseUrl}?lat={latitude}&lon={longitude}&appid={_apiKey}";

            var response = await _httpClient.GetAsync(url);

            // אם ה-API נכשל — מחזירים ברירת מחדל: שמיים בהירים (לא מחסמים את הניווט)
            if (!response.IsSuccessStatusCode)
                return new WeatherInfo { CloudPercentage = 0 };

            var json = await response.Content.ReadAsStringAsync();
            return ParseWeatherResponse(json);
        }

        private static WeatherInfo ParseWeatherResponse(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                //אחוז עננות (0 = שמיים בהירים, 100 = מעונן לחלוטין)
                var cloudPercentage = doc.RootElement
                    .GetProperty("clouds")
                    .GetProperty("all")
                    .GetInt32();

                return new WeatherInfo
                {
                    CloudPercentage = cloudPercentage
                };
            }
            catch (JsonException)
            {
                return new WeatherInfo { CloudPercentage = 0 };
            }
        }
    }
}
