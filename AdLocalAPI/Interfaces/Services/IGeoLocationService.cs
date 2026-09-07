using System.Threading.Tasks;

namespace AdLocalAPI.Interfaces.Services
{
    public interface IGeoLocationService
    {
        Task<(double lat, double lng, string municipio)?> GetLocationByIp(string ip);
    }
}
