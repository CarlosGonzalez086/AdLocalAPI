using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services.Interfaces;
using static AdLocalAPI.DTOs.PagosComercio;

namespace AdLocalAPI.Services
{
    public class CuentaBancariaComercioService : ICuentaBancariaComercioService
    {
        private readonly ICuentaBancariaComercioRepository _repository;
        private readonly IComercioRepository _comercioRepository;
        private readonly JwtContext _jwtContext;

        public CuentaBancariaComercioService(
            ICuentaBancariaComercioRepository repository,
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
                return (false, 0, "403", "Solo las cuentas de comercio pueden administrar cuentas bancarias.");

            var idComercio = comercioIdSolicitado > 0 ? comercioIdSolicitado : _jwtContext.GetComercioId();
            if (idComercio <= 0)
                return (false, 0, "400", "No se encontró un comercio asociado.");

            var comercio = await _comercioRepository.GetByIdAsync(idComercio);
            if (comercio == null)
                return (false, 0, "404", "Comercio no encontrado.");

            if (comercio.IdUsuario != _jwtContext.GetUserId())
                return (false, 0, "403", "No tienes permiso para gestionar cuentas de este comercio.");

            return (true, idComercio, "200", string.Empty);
        }

        public async Task<ApiResponse<IEnumerable<CuentaBancariaComercioResponseDto>>> ObtenerTodas(long comercioId = 0)
        {
            try
            {
                var auth = await ResolverComercioPropioAsync(comercioId);
                if (!auth.Ok)
                {
                    return ApiResponse<IEnumerable<CuentaBancariaComercioResponseDto>>.Error(auth.Codigo, auth.Mensaje);
                }

                var cuentas = await _repository.ObtenerTodasAsync(auth.IdComercio);
                var result = cuentas.Select(Mapear);

                return ApiResponse<IEnumerable<CuentaBancariaComercioResponseDto>>.Success(result, "Cuentas obtenidas correctamente.");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<CuentaBancariaComercioResponseDto>>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<CuentaBancariaComercioResponseDto>> Crear(CuentaBancariaComercioCreateDto dto, long comercioId = 0)
        {
            try
            {
                var auth = await ResolverComercioPropioAsync(comercioId);
                if (!auth.Ok)
                {
                    return ApiResponse<CuentaBancariaComercioResponseDto>.Error(auth.Codigo, auth.Mensaje);
                }

                var idComercio = auth.IdComercio;

                var validacion = ValidarCuenta(dto.Banco, dto.Beneficiario, dto.NumeroCuenta, dto.Clabe, dto.NumeroTarjeta);
                if (validacion != null)
                {
                    return ApiResponse<CuentaBancariaComercioResponseDto>.Error("400", validacion);
                }

                var tieneCuenta = await _repository.TieneCuentaActivaAsync(idComercio);
                var principal = !tieneCuenta || dto.Principal;

                if (principal)
                {
                    await _repository.QuitarPrincipalAsync(idComercio);
                }

                var cuenta = new CuentaBancariaComercio
                {
                    Uuid = Guid.NewGuid(),
                    IdComercio = idComercio,
                    Banco = dto.Banco.Trim(),
                    Beneficiario = dto.Beneficiario.Trim(),
                    NumeroCuenta = Limpiar(dto.NumeroCuenta),
                    Clabe = Limpiar(dto.Clabe),
                    NumeroTarjeta = Limpiar(dto.NumeroTarjeta),
                    Principal = principal,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                await _repository.CrearAsync(cuenta);

                return ApiResponse<CuentaBancariaComercioResponseDto>.Success(Mapear(cuenta), "Cuenta bancaria registrada correctamente.");
            }
            catch (Exception ex)
            {
                return ApiResponse<CuentaBancariaComercioResponseDto>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> Actualizar(Guid uuid, CuentaBancariaComercioUpdateDto dto, long comercioId = 0)
        {
            try
            {
                var auth = await ResolverComercioPropioAsync(comercioId);
                if (!auth.Ok)
                {
                    return ApiResponse<bool>.Error(auth.Codigo, auth.Mensaje);
                }

                var idComercio = auth.IdComercio;
                if (uuid == Guid.Empty)
                {
                    return ApiResponse<bool>.Error("400", "La cuenta es requerida.");
                }

                var cuenta = await _repository.ObtenerPorUuidAsync(idComercio, uuid);
                if (cuenta == null)
                {
                    return ApiResponse<bool>.Error("404", "La cuenta bancaria no existe.");
                }

                var validacion = ValidarCuenta(dto.Banco, dto.Beneficiario, dto.NumeroCuenta, dto.Clabe, dto.NumeroTarjeta);
                if (validacion != null)
                {
                    return ApiResponse<bool>.Error("400", validacion);
                }

                if (dto.Principal)
                {
                    await _repository.QuitarPrincipalAsync(idComercio, cuenta.Id);
                }

                cuenta.Banco = dto.Banco.Trim();
                cuenta.Beneficiario = dto.Beneficiario.Trim();
                cuenta.NumeroCuenta = Limpiar(dto.NumeroCuenta);
                cuenta.Clabe = Limpiar(dto.Clabe);
                cuenta.NumeroTarjeta = Limpiar(dto.NumeroTarjeta);
                cuenta.Principal = cuenta.Principal || dto.Principal;
                cuenta.Activo = dto.Activo;
                cuenta.FechaActualizacion = DateTime.UtcNow;

                await _repository.ActualizarAsync(cuenta);

                return ApiResponse<bool>.Success(true, "Cuenta bancaria actualizada correctamente.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> Eliminar(Guid uuid, long comercioId = 0)
        {
            try
            {
                var auth = await ResolverComercioPropioAsync(comercioId);
                if (!auth.Ok)
                {
                    return ApiResponse<bool>.Error(auth.Codigo, auth.Mensaje);
                }

                var idComercio = auth.IdComercio;
                var cuenta = await _repository.ObtenerPorUuidAsync(idComercio, uuid);
                if (cuenta == null)
                {
                    return ApiResponse<bool>.Error("404", "La cuenta bancaria no existe.");
                }

                var eraPrincipal = cuenta.Principal;
                cuenta.Activo = false;
                cuenta.Principal = false;
                cuenta.FechaActualizacion = DateTime.UtcNow;

                await _repository.ActualizarAsync(cuenta);

                if (eraPrincipal)
                {
                    var otra = await _repository.ObtenerPrimeraActivaAsync(idComercio, cuenta.Id);
                    if (otra != null)
                    {
                        otra.Principal = true;
                        otra.FechaActualizacion = DateTime.UtcNow;
                        await _repository.ActualizarAsync(otra);
                    }
                }

                return ApiResponse<bool>.Success(true, "Cuenta bancaria eliminada correctamente.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> EstablecerPrincipal(Guid uuid, long comercioId = 0)
        {
            try
            {
                var auth = await ResolverComercioPropioAsync(comercioId);
                if (!auth.Ok)
                {
                    return ApiResponse<bool>.Error(auth.Codigo, auth.Mensaje);
                }

                var idComercio = auth.IdComercio;
                var cuenta = await _repository.ObtenerPorUuidAsync(idComercio, uuid);
                if (cuenta == null)
                {
                    return ApiResponse<bool>.Error("404", "La cuenta bancaria no existe.");
                }

                if (!cuenta.Activo)
                {
                    return ApiResponse<bool>.Error("400", "No puedes establecer como principal una cuenta inactiva.");
                }

                if (cuenta.Principal)
                {
                    return ApiResponse<bool>.Success(true, "La cuenta ya es la principal.");
                }

                await _repository.QuitarPrincipalAsync(idComercio, cuenta.Id);

                cuenta.Principal = true;
                cuenta.FechaActualizacion = DateTime.UtcNow;

                await _repository.ActualizarAsync(cuenta);

                return ApiResponse<bool>.Success(true, "Cuenta principal actualizada correctamente.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Error("500", ex.Message);
            }
        }

        private static string? ValidarCuenta(string? banco, string? beneficiario, string? numeroCuenta, string? clabe, string? numeroTarjeta)
        {
            if (string.IsNullOrWhiteSpace(banco))
            {
                return "El banco es requerido.";
            }

            if (string.IsNullOrWhiteSpace(beneficiario))
            {
                return "El beneficiario es requerido.";
            }

            if (string.IsNullOrWhiteSpace(numeroCuenta) && string.IsNullOrWhiteSpace(clabe) && string.IsNullOrWhiteSpace(numeroTarjeta))
            {
                return "Debes proporcionar al menos una CLABE, número de cuenta o número de tarjeta.";
            }

            if (!string.IsNullOrWhiteSpace(clabe) && clabe.Trim().Length != 18)
            {
                return "La CLABE debe contener 18 dígitos.";
            }

            return null;
        }

        private static string? Limpiar(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static CuentaBancariaComercioResponseDto Mapear(CuentaBancariaComercio entity)
        {
            return new CuentaBancariaComercioResponseDto
            {
                Uuid = entity.Uuid,
                IdComercio = entity.IdComercio,
                Banco = entity.Banco,
                Beneficiario = entity.Beneficiario,
                NumeroCuenta = entity.NumeroCuenta,
                Clabe = entity.Clabe,
                NumeroTarjeta = entity.NumeroTarjeta,
                Principal = entity.Principal,
                Activo = entity.Activo,
                FechaCreacion = entity.FechaCreacion,
                FechaActualizacion = entity.FechaActualizacion
            };
        }
    }
}