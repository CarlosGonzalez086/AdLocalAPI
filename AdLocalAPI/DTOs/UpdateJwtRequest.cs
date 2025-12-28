namespace AdLocalAPI.DTOs
{
    public class UpdateJwtRequest
    {
        public string Email { get; set; } = string.Empty;
        public bool UpdateJWT { get; set; }
    }
}
