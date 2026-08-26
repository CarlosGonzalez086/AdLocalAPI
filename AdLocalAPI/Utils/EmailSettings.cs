namespace AdLocalAPI.Utils
{
    public class EmailSettings
    {
        public string Host { get; set; } = null!;
        public int Port { get; set; }
        public string User { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
    public class EmailSettingsSendGrid
    {
        public string ApiKey { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
    }
    public class EmailConfiguracionDto
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        public string User { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;

        public string From { get; set; } = string.Empty;

        public string FromNombre { get; set; } = string.Empty;
    }
}
