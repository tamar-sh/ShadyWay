using System.ComponentModel.DataAnnotations;

namespace ShadyWay.API.Dtos
{
    public class LoginDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "פורמט אימייל לא תקין.")]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }
}



