using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Repository;
using AdLocalAPI.Models;
using AdLocalAPI.Services.Interfaces;
using static AdLocalAPI.DTOs.PagosComercio;

namespace AdLocalAPI.Services
{
    public class ConfiguracionPagoComercioService
        : IConfiguracionPagoComercioService
    {
        private readonly IConfiguracionPagoComercioRepository _repository;
        private readonly JwtContext _jwtContext;

        public ConfiguracionPagoComercioService(
            IConfiguracionPagoComercioRepository repository,
            JwtContext jwtContext)
        {
            _repository = repository;
            _jwtContext = jwtContext;
        }

        public async Task<
            ApiResponse<ConfiguracionPagoComercioResponseDto?>
        > Obtener()
        {
            try
            {
                var idComercio =
                    _jwtContext.GetComercioId();

                if (idComercio <= 0)
                {
                    return ApiResponse<
                        ConfiguracionPagoComercioResponseDto?
                    >.Error(
                        "400",
                        "No se encontró un comercio asociado al usuario."
                    );
                }

                var configuracion =
                    await _repository.ObtenerPorComercioAsync(
                        idComercio
                    );

                if (configuracion == null)
                {
                    return ApiResponse<
                        ConfiguracionPagoComercioResponseDto?
                    >.Success(
                        null,
                        "El comercio todavía no tiene configuración de pagos."
                    );
                }

                return ApiResponse<
                    ConfiguracionPagoComercioResponseDto?
                >.Success(
                    Mapear(configuracion),
                    "Configuración obtenida correctamente."
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<
                    ConfiguracionPagoComercioResponseDto?
                >.Error(
                    "500",
                    ex.Message
                );
            }
        }

        public async Task<
            ApiResponse<ConfiguracionPagoComercioResponseDto>
        > Guardar(
            ConfiguracionPagoComercioDto dto)
        {
            try
            {
                var idComercio =
                    _jwtContext.GetComercioId();

                if (idComercio <= 0)
                {
                    return ApiResponse<
                        ConfiguracionPagoComercioResponseDto
                    >.Error(
                        "400",
                        "No se encontró un comercio asociado al usuario."
                    );
                }

                if (
                    !dto.AceptaEfectivo &&
                    !dto.AceptaTransferencia
                )
                {
                    return ApiResponse<
                        ConfiguracionPagoComercioResponseDto
                    >.Error(
                        "400",
                        "Debes habilitar al menos un método de pago."
                    );
                }

                if (
                    dto.InstruccionesTransferencia != null &&
                    dto.InstruccionesTransferencia.Length > 300
                )
                {
                    return ApiResponse<
                        ConfiguracionPagoComercioResponseDto
                    >.Error(
                        "400",
                        "Las instrucciones no pueden exceder los 300 caracteres."
                    );
                }

                var configuracion =
                    await _repository.ObtenerPorComercioAsync(
                        idComercio
                    );

                if (configuracion == null)
                {
                    configuracion =
                        new ConfiguracionPagoComercio
                        {
                            Uuid =
                                Guid.NewGuid(),

                            IdComercio =
                                idComercio,

                            AceptaEfectivo =
                                dto.AceptaEfectivo,

                            AceptaTransferencia =
                                dto.AceptaTransferencia,

                            InstruccionesTransferencia =
                                string.IsNullOrWhiteSpace(
                                    dto.InstruccionesTransferencia
                                )
                                    ? null
                                    : dto.InstruccionesTransferencia.Trim(),

                            Activo =
                                dto.Activo,

                            FechaCreacion =
                                DateTime.UtcNow
                        };

                    await _repository.CrearAsync(
                        configuracion
                    );
                }
                else
                {
                    configuracion.AceptaEfectivo =
                        dto.AceptaEfectivo;

                    configuracion.AceptaTransferencia =
                        dto.AceptaTransferencia;

                    configuracion.InstruccionesTransferencia =
                        string.IsNullOrWhiteSpace(
                            dto.InstruccionesTransferencia
                        )
                            ? null
                            : dto.InstruccionesTransferencia.Trim();

                    configuracion.Activo =
                        dto.Activo;

                    configuracion.FechaActualizacion =
                        DateTime.UtcNow;

                    await _repository.ActualizarAsync(
                        configuracion
                    );
                }

                return ApiResponse<
                    ConfiguracionPagoComercioResponseDto
                >.Success(
                    Mapear(configuracion),
                    "Configuración de pagos guardada correctamente."
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<
                    ConfiguracionPagoComercioResponseDto
                >.Error(
                    "500",
                    ex.Message
                );
            }
        }

        private static ConfiguracionPagoComercioResponseDto Mapear(
            ConfiguracionPagoComercio entity)
        {
            return new ConfiguracionPagoComercioResponseDto
            {
                Uuid =
                    entity.Uuid,

                IdComercio =
                    entity.IdComercio,

                AceptaEfectivo =
                    entity.AceptaEfectivo,

                AceptaTransferencia =
                    entity.AceptaTransferencia,

                InstruccionesTransferencia =
                    entity.InstruccionesTransferencia,

                Activo =
                    entity.Activo,

                FechaCreacion =
                    entity.FechaCreacion,

                FechaActualizacion =
                    entity.FechaActualizacion
            };
        }
    }
}