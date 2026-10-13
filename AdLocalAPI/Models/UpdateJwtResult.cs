namespace AdLocalAPI.Models
{
    public class UpdateJwtResult
    {
        public bool Success { get; set; }
        public string Codigo { get; set; } = "200";
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public Usuario? Usuario { get; set; }
    }

}
