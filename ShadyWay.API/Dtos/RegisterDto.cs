using System.ComponentModel.DataAnnotations;
namespace ShadyWay.API.Dtos
{
    public class RegisterDto
    {
        [Required]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "תעודת זהות חייבת להכיל בדיוק 9 ספרות.")]
        public string IdentityCard { get; set; } = "";

        [Required]
        [MaxLength(100, ErrorMessage = "שם מלא לא יעלה על 100 תווים.")]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress(ErrorMessage = "פורמט אימייל לא תקין.")]
        public string Email { get; set; } = "";

        [Required]
        [MinLength(8, ErrorMessage = "סיסמה חייבת להכיל לפחות 8 תווים.")]
        public string Password { get; set; } = "";

        [Range(1.0, 2.0, ErrorMessage = "העדפת צל חייבת להיות בין 1.0 ל-2.0.")]
        public float ShadowPreference { get; set; } = 1.2f;
    }
}


