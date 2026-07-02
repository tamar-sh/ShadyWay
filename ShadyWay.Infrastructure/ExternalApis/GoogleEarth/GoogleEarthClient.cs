using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ShadyWay.Infrastructure.ExternalApis.GoogleEarth
{
    public class GoogleEarthClient : IGoogleEarthClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _projectId;
        private readonly string _credentialPath;
        private readonly string _baseUrl;

        private readonly string _earthEngineScope;
        private readonly string _canopyHeightAssetId;
        private readonly int    _sampleScaleMeters;
        private readonly double _minTreeHeightMeters;
        private readonly double _minCanopyRadiusMeters;
        private readonly double _canopyRadiusFactor;

        public GoogleEarthClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient             = httpClient;
            _projectId              = configuration["GoogleEarth:ProjectId"]!;
            _credentialPath         = configuration["GoogleEarth:ServiceAccountKeyPath"]!;
            _baseUrl                = configuration["GoogleEarth:BaseUrl"]!;
            _earthEngineScope       = configuration["GoogleEarth:EarthEngineScope"]!;
            _canopyHeightAssetId    = configuration["GoogleEarth:CanopyHeightAssetId"]!;
            _sampleScaleMeters      = configuration.GetValue<int>("GoogleEarth:SampleScaleMeters");
            _minTreeHeightMeters    = configuration.GetValue<double>("GoogleEarth:MinTreeHeightMeters");
            _minCanopyRadiusMeters  = configuration.GetValue<double>("GoogleEarth:MinCanopyRadiusMeters");
            _canopyRadiusFactor     = configuration.GetValue<double>("GoogleEarth:CanopyRadiusFactor");
        }
        //קבלת Token
        private async Task<string> GetAccessTokenAsync()
        {
            var credential = GoogleCredential
                .FromFile(_credentialPath)
                .CreateScoped(_earthEngineScope);
            return await credential.UnderlyingCredential
                .GetAccessTokenForRequestAsync();
        }
        //הוספת ה-Token ל-header של הבקשה
        private async Task AddAuthHeaderAsync()
        {
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
        
        public async Task<IEnumerable<TreeInfo>> GetTreesInAreaAsync(
            double minLat, double minLon,
            double maxLat, double maxLon)
        {
            if (!File.Exists(_credentialPath))
                return Enumerable.Empty<TreeInfo>();
            try
            {
                await AddAuthHeaderAsync();
                var requestBody = new
                {
                    expression = new
                    {
                        result = "0",
                        values = new Dictionary<string, object>
                        {
                            ["0"] = BuildCanopySampleExpression(minLat, minLon, maxLat, maxLon)
                        }
                    }
                };
                // שולח בקשה ל-Earth Engine ומקבל את התשובה כטקסט
                var responseBody = await PostToEarthEngineAsync(requestBody);
                // ממיר את התשובה לרשימת עצים   
                return ParseTreesFromCanopyJson(responseBody);
            }
            catch (HttpRequestException)
            {
                return Enumerable.Empty<TreeInfo>();
            }
        }

        // שולחת בקשה ל-Earth Engine ומחזירה את גוף התשובה כטקסט
        private async Task<string> PostToEarthEngineAsync(object requestBody)
        {
            var json     = JsonSerializer.Serialize(requestBody);
            var content  = new StringContent(json, Encoding.UTF8, "application/json");
            var url      = $"{_baseUrl}projects/{_projectId}/table:computeFeatures";
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private object BuildCanopySampleExpression(
            double minLat, double minLon,
            double maxLat, double maxLon)
        {
            //functionInvocationValue = new-  פונקציה ראשית שאני רוצה שגוגל ארץ יפעילו על השרתים שלהם ואני רוצה את הערך המוחזר מהפונקציה
            //לפונקציה הראשית קוראים דגימת תמונה - functionName = "Image.sample"
            //וכדי שהיא תעבוד היא צריכה נתונים כמו:
            //1) איזה תמונה - image = new
            //2) ואיפה -  region = new 

            return new
            {
                //זה פונקציה  הראשית שאני רוצה שתפעיל לי ואחרי ההפעלה אני רוצה לקבל את הערך הזה
                functionInvocationValue = new
                {
                    //זה השם של הפונקצייה  הראשית שאני רוצה שתפעיל לי
                    functionName = "Image.sample",
                    // אלו הנתונים (הפרמטרים) שאתה צריך בשביל זה:  
                    arguments = new
                    {
                        image = new
                        {
                            functionInvocationValue = new
                            {
                                functionName = "Image.load",
                                arguments   = new
                                {
                                    id = new { constantValue = _canopyHeightAssetId }
                                }
                            }
                        },

                        region = new
                        {
                            functionInvocationValue = new
                            {
                                functionName = "GeometryConstructors.Polygon",
                                arguments   = new
                                {
                                    coordinates = new
                                    {
                                        constantValue = new[]
                                        {
                                            new[]
                                            {
                                                new[] { minLon, minLat },
                                                new[] { maxLon, minLat },
                                                new[] { maxLon, maxLat },
                                                new[] { minLon, maxLat },
                                                new[] { minLon, minLat }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        // דגימה כל כמה מטרים — מאזן בין דיוק לכמות נקודות
                        scale = new { constantValue = _sampleScaleMeters },
                        // כלול את הקואורדינטות בתשובה (לא רק הערכים)
                        geometries = new { constantValue = true }
                    }
                }
            };
        }

        // ממיר תשובת Image.sample לרשימת TreeInfo
        // כל פיקסל מעל הגובה המינימלי הופך לעץ — מסנן דשא ושיחים נמוכים
        private IEnumerable<TreeInfo> ParseTreesFromCanopyJson(string json)
        {
            var trees = new List<TreeInfo>();
            try
            {
                //טעינת המחרוזת וגישה לנתונים שלה בצורה יעילה
                using var doc = JsonDocument.Parse(json);
                //מכיל מערך של מאפיינים
                var features = doc.RootElement
                    .GetProperty("features")
                    .EnumerateArray();

                foreach (var feature in features)
                {
                    //מכיל את המידע הסטטיסטי בשביל גובה
                    var props    = feature.GetProperty("properties");
                    //הקואורדינטות
                    var geometry = feature.GetProperty("geometry");
                    
                    //הגובה של הצמחייה — השדה המספרי הראשון שנמצא בתוך properties
                    var heightProp = props.EnumerateObject()
                        .FirstOrDefault(p => p.Value.ValueKind == JsonValueKind.Number);
                    double height = heightProp.Value.ValueKind == JsonValueKind.Number
                        ? heightProp.Value.GetDouble()
                        : 0;

                    // מסנן צמחייה נמוכה — רק עצים ממשיים
                    if (height >= _minTreeHeightMeters &&
                        geometry.TryGetProperty("coordinates", out var coords) &&
                        coords.ValueKind == JsonValueKind.Array)
                    {
                        var pt = coords.EnumerateArray().ToArray();
                        if (pt.Length >= 2)
                        {
                            trees.Add(new TreeInfo
                            {
                                Longitude          = pt[0].GetDouble(),
                                Latitude           = pt[1].GetDouble(),
                                HeightMeters       = height,
                                // רדיוס צמרת מוערך לפי יחס מהגובה
                                CanopyRadiusMeters = Math.Max(_minCanopyRadiusMeters, height * _canopyRadiusFactor)
                            });
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // תשובה לא תקינה מ-Earth Engine — מחזירים את העצים שכבר פוענחו בהצלחה, בלי לקרוס
            }

            return trees;
        }
    }
}
