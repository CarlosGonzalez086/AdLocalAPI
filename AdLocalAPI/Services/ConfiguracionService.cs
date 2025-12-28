using AdLocalAPI.Constants;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Models;
using AdLocalAPI.Dictionaries;

namespace AdLocalAPI.Services
{
    public class ConfiguracionService : IConfiguracionService
    {
        private readonly IConfiguracionRepository _repository;
        public ConfiguracionService(IConfiguracionRepository repository)
        {
            _repository = repository;
        }
        public async Task<ApiResponse<ConfiguracionSistema>> CrearOActualizarAsync(ConfiguracionSistemaDto dto)
        {
            if (!ConfiguracionSistemaDictionary.Data.ContainsKey(dto.Key))
            {
                return ApiResponse<ConfiguracionSistema>.Error(
                    "400",
                    $"La configuración '{dto.Key}' no está registrada en el sistema."
                );
            }

            var meta = ConfiguracionSistemaDictionary.Data[dto.Key];
            var existente = await _repository.ObtenerPorKeyAsync(dto.Key);

            ConfiguracionSistema resultado;

            if (existente == null)
            {
                resultado = await _repository.InsertarAsync(new ConfiguracionSistema
                {
                    Key = dto.Key,
                    Descripcion = meta.Description,
                    Tipo = meta.Tipo,
                    Val = dto.Val,
                    Actualizado = DateTime.UtcNow
                });
            }
            else
            {
                existente.Val = dto.Val;
                existente.Actualizado = DateTime.UtcNow;
                resultado = await _repository.ActualizarAsync(existente);
            }

            return ApiResponse<ConfiguracionSistema>.Success(
                resultado,
                "Configuración guardada correctamente"
            );
        }
        public async Task<ApiResponse<List<ConfiguracionSistema>>> ObtenerTodosAsync()
        {
            var lista = await _repository.ObtenerTodosAsync();
            return ApiResponse<List<ConfiguracionSistema>>.Success(lista);
        }
        public async Task<ApiResponse<List<ConfiguracionSistema>>> RegistrarStripeAsync(StripeConfiguracionDto dto)
        {
            var resultado = new List<ConfiguracionSistema>();

            var acciones = new[]
            {
                new ConfiguracionSistemaDto { Key = ConfiguracionKeys.StripePublishableKey, Val = dto.PublishableKey },
                new ConfiguracionSistemaDto { Key = ConfiguracionKeys.StripeSecretKey, Val = dto.SecretKey },
                new ConfiguracionSistemaDto { Key = ConfiguracionKeys.StripeCommissionPercentage, Val = dto.CommissionPercentage },
                new ConfiguracionSistemaDto { Key = ConfiguracionKeys.StripeCommissionFixed, Val = dto.CommissionFixed }
            };

            foreach (var item in acciones)
            {
                var res = await CrearOActualizarAsync(item);

                if (res.Codigo != "200")
                    return ApiResponse<List<ConfiguracionSistema>>.Error(
                        res.Codigo,
                        res.Mensaje
                    );

                resultado.Add(res.Respuesta);
            }

            return ApiResponse<List<ConfiguracionSistema>>.Success(
                resultado,
                "Configuración de Stripe registrada correctamente"
            );
        }
    }
}
