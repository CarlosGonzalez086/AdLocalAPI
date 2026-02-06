using AdLocalAPI.DTOs;

namespace AdLocalAPI.Services
{
    public class GeoLocationService
    {
        private readonly AppConfigState _config;
        private readonly HttpClient _http;

        public GeoLocationService(AppConfigState config, HttpClient http)
        {
            _config = config;
            _http = http;
        }

        public async Task<(double lat, double lng, string municipio)?> GetLocationByIp(string ip)
        {
            if (string.IsNullOrEmpty(_config.Ip2LocationKey))
                return null;

            var url =
                $"https://api.ip2location.io/?key={_config.Ip2LocationKey}&ip={ip}";

            var response = await _http.GetFromJsonAsync<Ip2LocationResponse>(url);

            if (response == null) return null;

            return (
                response.latitude,
                response.longitude,
                response.city_name
            );
        }
    }

}
