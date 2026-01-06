using AdLocalAPI.Interfaces;
using AdLocalAPI.Interfaces.Tarjetas;

namespace AdLocalAPI.UseCases
{
    public class EliminarTarjetaUseCase
    {
        private readonly ITarjetaRepository _repo;
        private readonly IStripeService _stripe;

        public EliminarTarjetaUseCase(
            ITarjetaRepository repo,
            IStripeService stripe)
        {
            _repo = repo;
            _stripe = stripe;
        }

        public async Task Execute(long userId, long tarjetaId)
        {
            var tarjeta = await _repo.GetById(tarjetaId, userId);
            if (tarjeta == null) throw new Exception("Tarjeta no encontrada");

            tarjeta.Status = false;
            tarjeta.DeletedAt = DateTime.UtcNow;
            tarjeta.IsDefault = false;

            await _stripe.Detach(tarjeta.StripePaymentMethodId);

            await _repo.Update(tarjeta);
            await _repo.Save();
        }
    }

}
