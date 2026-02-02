using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Interfaces.Tarjetas;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;

namespace AdLocalAPI.Services
{
    public class TarjetaService : ITarjetaService
    {
        private readonly ITarjetaRepository _repository;
        private readonly IStripeService _stripe;
        private readonly UsuarioRepository _UserRepository;
        private readonly JwtContext _jwtContext;

        public TarjetaService(
            ITarjetaRepository repository,
            IStripeService stripe, JwtContext jwtContext, UsuarioRepository UserRepository)
        {
            _repository = repository;
            _stripe = stripe;
            _jwtContext = jwtContext;
            _UserRepository = UserRepository;
        }

        public async Task<ApiResponse<object>> CrearTarjeta(
            CrearTarjetaDto dto)
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                var tarjetasExistentes = await _repository.GetByUser(idUser);
                if (tarjetasExistentes.Count >= 3)
                {
                    return ApiResponse<object>.Error("400", "No puedes registrar más de 3 tarjetas");
                }
                var user = await _UserRepository.GetByIdAsync(idUser);
                if (string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    var customerId = await _stripe.CreateCustomer(user.Email);
                    user.StripeCustomerId = customerId;
                    await _UserRepository.UpdateAsync(user);
                }

                await _stripe.AttachToCustomer(dto.PaymentMethodId, user.StripeCustomerId);
                var pm = await _stripe.GetPaymentMethod(dto.PaymentMethodId);

                if (dto.IsDefault)
                {
                    await _repository.RemoveDefaults(idUser);
                    await _stripe.SetDefaultPaymentMethod(user.StripeCustomerId, pm.Id);
                }

                var tarjeta = new Tarjeta
                {
                    UserId = idUser,
                    StripeCustomerId = user.StripeCustomerId,
                    StripePaymentMethodId = pm.Id,
                    Brand = pm.Card.Brand,
                    Last4 = pm.Card.Last4,
                    ExpMonth = (int)pm.Card.ExpMonth,
                    ExpYear = (int)pm.Card.ExpYear,
                    CardType = pm.Card.Funding,
                    IsDefault = dto.IsDefault,
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.Add(tarjeta);
                await _repository.Save();

                return ApiResponse<object>.Success(null, "Tarjeta registrada correctamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<object>> SetDefault(long tarjetaId)
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                var user = await _UserRepository.GetByIdAsync(idUser);
                var tarjeta = await _repository.GetById(tarjetaId, idUser);

                if (tarjeta == null)
                    return ApiResponse<object>.Error("404", "Tarjeta no encontrada");


                if (tarjeta.IsDefault)
                    return ApiResponse<object>.Success(null, "Esta tarjeta ya es la principal");


                var tarjetasUsuario = await _repository.GetByUser(idUser);
                foreach (var t in tarjetasUsuario)
                {
                    if (t.IsDefault)
                    {
                        t.IsDefault = false;
                        await _repository.Update(t);
                    }
                }


                await _stripe.SetDefaultPaymentMethod(user.StripeCustomerId, tarjeta.StripePaymentMethodId);

    
                tarjeta.IsDefault = true;
                await _repository.Update(tarjeta);

                return ApiResponse<object>.Success(null, "Tarjeta establecida como principal");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }


        public async Task<ApiResponse<object>> EliminarTarjeta(
            long tarjetaId)
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                var tarjeta = await _repository.GetById(tarjetaId, idUser);

                if (tarjeta == null)
                    return ApiResponse<object>.Error("404", "Tarjeta no encontrada");

                tarjeta.Status = false;
                tarjeta.DeletedAt = DateTime.UtcNow;
                tarjeta.IsDefault = false;

                await _stripe.Detach(tarjeta.StripePaymentMethodId);
                await _repository.Update(tarjeta);

                return ApiResponse<object>.Success(null, "Tarjeta eliminada correctamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
        public async Task<ApiResponse<List<TarjetaDto>>> Listar()
        {
            try
            {
                long idUser = _jwtContext.GetUserId();

                var tarjetas = await _repository.GetByUser(idUser);

                var result = tarjetas.Select(t => new TarjetaDto
                {
                    Id = t.Id,
                    Brand = t.Brand,
                    Last4 = t.Last4,
                    ExpMonth = t.ExpMonth,
                    ExpYear = t.ExpYear,
                    CardType = t.CardType,
                    IsDefault = t.IsDefault,
                    StripePaymentMethodId = t.StripePaymentMethodId,
                }).ToList();

                return ApiResponse<List<TarjetaDto>>.Success(
                    result,
                    "Tarjetas obtenidas correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<List<TarjetaDto>>.Error("500", ex.Message);
            }
        }

    }

}
