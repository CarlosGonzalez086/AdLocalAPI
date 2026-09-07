namespace AdLocalAPI.Services
{
    public class AppConfigState
    {
        public string Ip2LocationKey { get; private set; } = string.Empty;

        public void SetIp2LocationKey(string key)
        {
            Ip2LocationKey = key;
        }
    }
}
