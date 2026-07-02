using System.ComponentModel.DataAnnotations;

namespace ShadyWay.API.Dtos
{
    public class RouteRequestDto
    {
        [Range(-90, 90, ErrorMessage = "קו רוחב לא תקין")]
        public double StartLatitude { get; set; }// קו רוחב של נקודת המוצא

        [Range(-180, 180, ErrorMessage = "קו אורך לא תקין")]
        public double StartLongitude { get; set; }// קו אורך של נקודת המוצא

        [Range(-90, 90, ErrorMessage = "קו רוחב לא תקין")]
        public double EndLatitude { get; set; }

        [Range(-180, 180, ErrorMessage = "קו אורך לא תקין")]
        public double EndLongitude { get; set; }

        [Range(1.0, 2.0, ErrorMessage = "העדפת צל חייבת להיות בין 1 ל-2")]
        public double ShadowPreference { get; set; } = 1.2;

        public DateTime? UtcDateTime { get; set; }
    }
}




