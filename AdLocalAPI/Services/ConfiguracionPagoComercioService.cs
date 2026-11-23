using AdLocalAPI.Helpers;
using AdLocalAPI.Interfaces.Repository;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;
using static AdLocalAPI.DTOs.PagosComercio;

namespace AdLocalAPI.Services
{
    public class ConfiguracionPagoComercioService
        : IConfiguracionPagoComercioService
    {
        private readonly IConfiguracionPagoComercioRepository _repository;
        private readonly IComercioRepository _comercioRepository;
        private readonly JwtContext _jwtContext;

        public ConfiguracionPagoComercioService(
            IConfiguracionPagoComercioRepository repository,
            IComercioRepository comercioRepository,
            JwtContext jwtContext)
        {
            _repository = repository;
            _comercioRepository = comercioRepository;
            _jwtContext = jwtContext;
        }

        private async Task<(bool Ok, long IdComercio, string Codigo, string Mensaje)> ResolverComercioPropioAsync(long comercioIdSolicitado)
        {
            if (!_jwtContext.EstaAutenticado())
                return (false, 0, "401", "Usuario no autenticado.");

            if (!_jwtContext.EsComercio())
                return (false, 0, "403", "Solo las cuentas de comercio pueden administrar métodos de pago.");

            var idComercio = comercioIdSolicitado > 0 ? comercioIdSolicitado : _jwtContext.GetComercioId();
            if (idComercio <= 0)
                return (false, 0, "400", "No se encontró un comercio asociado al usuario.");

            var comercio = await _comercioRepository.GetByIdAsync(idComercio);
            if (comercio == null)
                return (false, 0, "404", "Comercio no encontrado.");

            if (comercio.IdUsuario != _jwtContext.GetUserId())
                return (false, 0, "403", "No tienes permiso para configurar los pagos de este comercio.");

            return (true, idComercio, "200", string.Empty);
        }

        public async Task<
            ApiResponse<ConfiguracionPagoComercioResponseDto?>
        > Obtener(long comercioId = 0)
        {
            try
            {
                var auth = await ResolverComercioPropioAsync(comercioId);
                if (!auth.Ok)
                {
                    return ApiResponse<
                        ConfiguracionPagoComercioResponseDto?
                    >.Error(auth.Codigo, auth.Mensaje);
                }

                var configuracion =
                    await _repository.ObtenerPorComercioAsync(
                        auth.IdComercio
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
            ConfiguracionPagoComercioDto dto,
            long comercioId = 0)
        {
            try
            {
                var auth = await ResolverComercioPropioAsync(comercioId);
                if (!auth.Ok)
                {
                    return ApiResponse<
                        ConfiguracionPagoComercioResponseDto
                    >.Error(auth.Codigo, auth.Mensaje);
                }

                var idComercio = auth.IdComercio;

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

                if (dto.CostoEnvio < 0 || dto.CompraMinimaEnvioGratis < 0)
                    return ApiResponse<ConfiguracionPagoComercioResponseDto>.Error("400", "La tarifa de envío no puede ser negativa.");

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

                            CostoEnvio = dto.CostoEnvio,
                            CompraMinimaEnvioGratis = dto.CompraMinimaEnvioGratis,

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

                    configuracion.CostoEnvio = dto.CostoEnvio;
                    configuracion.CompraMinimaEnvioGratis = dto.CompraMinimaEnvioGratis;

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

                CostoEnvio = entity.CostoEnvio,
                CompraMinimaEnvioGratis = entity.CompraMinimaEnvioGratis,

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
