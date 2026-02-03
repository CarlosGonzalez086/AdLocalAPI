using AdLocalAPI.Constants;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services
{
    public class StripeConfigProvider
    {
        public string SecretKey { get; private set; } = null!;
        public string PublishableKey { get; private set; } = null!;
        public decimal CommissionPercentage { get; private set; }
        public decimal CommissionFixed { get; private set; }

        public void Load(IEnumerable<ConfiguracionSistema> configs)
        {
            SecretKey = configs
                .First(x => x.Key == ConfiguracionKeys.StripeSecretKey)
                .Val;

            PublishableKey = configs
                .First(x => x.Key == ConfiguracionKeys.StripePublishableKey)
                .Val;

            CommissionPercentage = decimal.Parse(
                configs.First(x => x.Key == ConfiguracionKeys.StripeCommissionPercentage).Val
            );

            CommissionFixed = decimal.Parse(
                configs.First(x => x.Key == ConfiguracionKeys.StripeCommissionFixed).Val
            );
        }
    }

}
