using AdLocalAPI.Constants;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services
{
    public class ClavesConfigProvider
    {
        public string Ip2LocationKey { get; private set; } = null!;
        public void Load(IEnumerable<ConfiguracionSistema> configs)
        {
            Ip2LocationKey = configs
                .First(x => x.Key == ConfiguracionKeys.Ip2LocationKey)
                .Val;
        }
    }
}
