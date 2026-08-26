using AdLocalAPI.Constants;
using AdLocalAPI.Dictionaries;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;

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
        public async Task<ApiResponse<List<ConfiguracionSistema>>> RegistrarCrearClavesAsync(ClavesConfigDto dto)
        {
            var resultado = new List<ConfiguracionSistema>();

            var acciones = new[]
            {
                new ConfiguracionSistemaDto { Key = ConfiguracionKeys.Ip2LocationKey, Val = dto.Ip2LocationKey },
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
                "Configuración de claves registrada correctamente"
            );
        }
        public async Task<ApiResponse<List<ConfiguracionSistema>>>RegistrarComisionMarketplaceAsync(ComisionMarketplaceDto dto)
        {
            if (dto == null)
            {
                return ApiResponse<List<ConfiguracionSistema>>
                    .Error(
                        "400",
                        "La configuración de comisión es requerida."
                    );
            }

            if (dto.Porcentaje < 0 || dto.Porcentaje > 100)
            {
                return ApiResponse<List<ConfiguracionSistema>>
                    .Error(
                        "400",
                        "El porcentaje de comisión debe estar entre 0 y 100."
                    );
            }

            if (dto.MontoFijo < 0)
            {
                return ApiResponse<List<ConfiguracionSistema>>
                    .Error(
                        "400",
                        "El monto fijo de comisión no puede ser negativo."
                    );
            }

            var resultado =
                new List<ConfiguracionSistema>();

            var acciones = new[]
            {
        new ConfiguracionSistemaDto
        {
            Key =
                ConfiguracionKeys
                    .MarketplaceCommissionPercentage,

            Val =
                dto.Porcentaje.ToString(
                    System.Globalization.CultureInfo.InvariantCulture
                )
        },

        new ConfiguracionSistemaDto
        {
            Key =
                ConfiguracionKeys
                    .MarketplaceCommissionFixed,

            Val =
                dto.MontoFijo.ToString(
                    System.Globalization.CultureInfo.InvariantCulture
                )
        },

        new ConfiguracionSistemaDto
        {
            Key =
                ConfiguracionKeys
                    .MarketplaceCommissionEnabled,

            Val =
                dto.Activa.ToString()
        }
    };

            foreach (var item in acciones)
            {
                var res =
                    await CrearOActualizarAsync(
                        item
                    );

                if (res.Codigo != "200")
                {
                    return ApiResponse<List<ConfiguracionSistema>>
                        .Error(
                            res.Codigo,
                            res.Mensaje
                        );
                }

                if (res.Respuesta != null)
                {
                    resultado.Add(
                        res.Respuesta
                    );
                }
            }

            return ApiResponse<List<ConfiguracionSistema>>
                .Success(
                    resultado,
                    "Configuración de comisión registrada correctamente."
                );
        }
        public async Task<ApiResponse<List<ConfiguracionSistema>>>RegistrarEmailAsync(EmailConfiguracionDto dto)
        {
            if (dto == null)
            {
                return ApiResponse<List<ConfiguracionSistema>>
                    .Error(
                        "400",
                        "La configuración de correo es requerida."
                    );
            }

            if (string.IsNullOrWhiteSpace(dto.Host))
            {
                return ApiResponse<List<ConfiguracionSistema>>
                    .Error(
                        "400",
                        "El servidor SMTP es requerido."
                    );
            }

            if (dto.Port <= 0 || dto.Port > 65535)
            {
                return ApiResponse<List<ConfiguracionSistema>>
                    .Error(
                        "400",
                        "El puerto SMTP no es válido."
                    );
            }

            if (string.IsNullOrWhiteSpace(dto.From))
            {
                return ApiResponse<List<ConfiguracionSistema>>
                    .Error(
                        "400",
                        "El correo remitente es requerido."
                    );
            }

            var resultado =
                new List<ConfiguracionSistema>();

            var acciones = new[]
            {
                new ConfiguracionSistemaDto
                {
                    Key = ConfiguracionKeys.EmailHost,
                    Val = dto.Host.Trim()
                },

                new ConfiguracionSistemaDto
                {
                    Key = ConfiguracionKeys.EmailPort,
                    Val = dto.Port.ToString(
                        System.Globalization.CultureInfo.InvariantCulture
                    )
                },

                new ConfiguracionSistemaDto
                {
                    Key = ConfiguracionKeys.EmailUser,
                    Val = dto.User?.Trim() ?? string.Empty
                },

                new ConfiguracionSistemaDto
                {
                    Key = ConfiguracionKeys.EmailKey,
                    Val = dto.Key ?? string.Empty
                },

                new ConfiguracionSistemaDto
                {
                    Key = ConfiguracionKeys.EmailFrom,
                    Val = dto.From.Trim()
                },

                new ConfiguracionSistemaDto
                {
                    Key = ConfiguracionKeys.EmailFromNombre,
                    Val = dto.FromNombre?.Trim() ?? string.Empty
                }
            };

            foreach (var item in acciones)
            {
                var res =
                    await CrearOActualizarAsync(
                        item
                    );

                if (res.Codigo != "200")
                {
                    return ApiResponse<List<ConfiguracionSistema>>
                        .Error(
                            res.Codigo,
                            res.Mensaje
                        );
                }

                if (res.Respuesta != null)
                {
                    resultado.Add(
                        res.Respuesta
                    );
                }
            }

            return ApiResponse<List<ConfiguracionSistema>>
                .Success(
                    resultado,
                    "Configuración de correo registrada correctamente."
                );
        }
    }
}
