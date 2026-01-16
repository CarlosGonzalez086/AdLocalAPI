namespace AdLocalAPI.DTOs
{
    public class ChangePasswordDto
    {
        public string PasswordActual { get; set; }
        public string PasswordNueva { get; set; }
    }
    public class NewPasswordDto
    {
        public string PasswordNueva { get; set; }
        public string Codigo { get; set; }
    }
    public class EmailDto
    {
        public string Email { get; set; }
    }

}
