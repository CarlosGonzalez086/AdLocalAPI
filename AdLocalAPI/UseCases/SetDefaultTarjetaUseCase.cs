using AdLocalAPI.Interfaces;
using AdLocalAPI.Interfaces.Tarjetas;

namespace AdLocalAPI.UseCases
{
    public class SetDefaultTarjetaUseCase
    {
        private readonly ITarjetaRepository _repo;
        private readonly IStripeService _stripe;

        public SetDefaultTarjetaUseCase(
            ITarjetaRepository repo,
            IStripeService stripe)
        {
            _repo = repo;
            _stripe = stripe;
        }

        public async Task Execute(long userId, string customerId, long tarjetaId)
        {
            var tarjeta = await _repo.GetById(tarjetaId, userId);
            if (tarjeta == null) throw new Exception("Tarjeta no encontrada");

            await _repo.RemoveDefaults(userId);

            tarjeta.IsDefault = true;
            await _stripe.SetDefault(customerId, tarjeta.StripePaymentMethodId);

            await _repo.Update(tarjeta);
            await _repo.Save();
        }
    }

}
