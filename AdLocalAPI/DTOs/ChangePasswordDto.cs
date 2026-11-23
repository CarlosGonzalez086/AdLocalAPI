namespace AdLocalAPI.DTOs
{
    public class ChangePasswordDto
    {
        public string PasswordActual { get; set; } = string.Empty;
        public string PasswordNueva { get; set; } = string.Empty;
    }
    public class NewPasswordDto
    {
        public string PasswordNueva { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string? Token { get; set; }
        public string? Email { get; set; }
    }
    public class EmailDto
    {
        public string Email { get; set; } = string.Empty;
    }

}
