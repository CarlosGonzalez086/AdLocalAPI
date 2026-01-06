using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Interfaces.Tarjetas;
using AdLocalAPI.Models;

namespace AdLocalAPI.UseCases
{
    public class CrearTarjetaUseCase
    {
        private readonly ITarjetaRepository _repo;
        private readonly IStripeService _stripe;

        public CrearTarjetaUseCase(
            ITarjetaRepository repo,
            IStripeService stripe)
        {
            _repo = repo;
            _stripe = stripe;
        }

        public async Task Execute(long userId, string stripeCustomerId, CrearTarjetaDto dto)
        {
            await _stripe.AttachToCustomer(dto.PaymentMethodId, stripeCustomerId);

            var pm = await _stripe.GetPaymentMethod(dto.PaymentMethodId);
            var card = pm.Card;

            if (dto.IsDefault)
            {
                await _repo.RemoveDefaults(userId);
                await _stripe.SetDefault(stripeCustomerId, pm.Id);
            }

            var tarjeta = new Tarjeta
            {
                UserId = userId,
                StripeCustomerId = stripeCustomerId,
                StripePaymentMethodId = pm.Id,
                Brand = card.Brand,
                Last4 = card.Last4,
                ExpMonth = (int)card.ExpMonth,
                ExpYear = (int)card.ExpYear,
                CardType = card.Funding,
                IsDefault = dto.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.Add(tarjeta);
            await _repo.Save();
        }
    }
}
