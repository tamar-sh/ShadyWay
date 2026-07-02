namespace ShadyWay.API.Dtos
{
    public class AuthResponseDto
    {
        public string Token           { get; set; } = "";
        public string FullName        { get; set; } = "";
        public string Email           { get; set; } = "";
        public float  ShadowPreference { get; set; } = 1.2f;
    }
}
