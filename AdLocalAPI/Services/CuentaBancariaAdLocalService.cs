using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.Services;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;

namespace AdLocalAPI.Services
{
    public class CuentaBancariaAdLocalService : ICuentaBancariaAdLocalService
    {
        private readonly ICuentaBancariaAdLocalRepository _repository;

        public CuentaBancariaAdLocalService(ICuentaBancariaAdLocalRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<List<CuentaBancariaAdLocalDto>>> ListarAsync()
        {
            var cuentas = await _repository.ObtenerTodasAsync();
            return ApiResponse<List<CuentaBancariaAdLocalDto>>.Success(cuentas.Select(Mapear).ToList());
        }

        public async Task<ApiResponse<CuentaBancariaAdLocalDto>> ObtenerPrincipalAsync()
        {
            var cuenta = await _repository.ObtenerPrincipalAsync();
            if (cuenta == null)
            {
                return ApiResponse<CuentaBancariaAdLocalDto>.Error("404", "ADLocal todavía no ha configurado una cuenta bancaria.");
            }

            return ApiResponse<CuentaBancariaAdLocalDto>.Success(Mapear(cuenta));
        }

        public async Task<ApiResponse<CuentaBancariaAdLocalDto>> CrearAsync(GuardarCuentaBancariaAdLocalDto dto)
        {
            var error = Validar(dto);
            if (error != null)
            {
                return ApiResponse<CuentaBancariaAdLocalDto>.Error("400", error);
            }

            if (dto.Principal)
            {
                await _repository.QuitarPrincipalAsync();
            }

            var cuenta = new CuentaBancariaAdLocal
            {
                Banco = dto.Banco.Trim(),
                Beneficiario = dto.Beneficiario.Trim(),
                NumeroCuenta = Limpiar(dto.NumeroCuenta),
                Clabe = Limpiar(dto.Clabe),
                NumeroTarjeta = Limpiar(dto.NumeroTarjeta),
                Instrucciones = dto.Instrucciones?.Trim(),
                Principal = dto.Principal,
                Activo = true
            };

            await _repository.CrearAsync(cuenta);

            return ApiResponse<CuentaBancariaAdLocalDto>.Success(Mapear(cuenta), "Cuenta registrada.");
        }

        public async Task<ApiResponse<CuentaBancariaAdLocalDto>> ActualizarAsync(Guid uuid, GuardarCuentaBancariaAdLocalDto dto)
        {
            var cuenta = await _repository.ObtenerPorUuidAsync(uuid);
            if (cuenta == null)
            {
                return ApiResponse<CuentaBancariaAdLocalDto>.Error("404", "Cuenta bancaria no encontrada.");
            }

            var error = Validar(dto);
            if (error != null)
            {
                return ApiResponse<CuentaBancariaAdLocalDto>.Error("400", error);
            }

            if (dto.Principal)
            {
                await _repository.QuitarPrincipalAsync(cuenta.Id);
            }

            cuenta.Banco = dto.Banco.Trim();
            cuenta.Beneficiario = dto.Beneficiario.Trim();
            cuenta.NumeroCuenta = Limpiar(dto.NumeroCuenta);
            cuenta.Clabe = Limpiar(dto.Clabe);
            cuenta.NumeroTarjeta = Limpiar(dto.NumeroTarjeta);
            cuenta.Instrucciones = dto.Instrucciones?.Trim();
            cuenta.Principal = dto.Principal;
            cuenta.FechaActualizacion = DateTime.UtcNow;

            await _repository.ActualizarAsync(cuenta);

            return ApiResponse<CuentaBancariaAdLocalDto>.Success(Mapear(cuenta), "Cuenta actualizada.");
        }

        public async Task<ApiResponse<object>> CambiarEstadoAsync(Guid uuid)
        {
            var cuenta = await _repository.ObtenerPorUuidAsync(uuid);
            if (cuenta == null)
            {
                return ApiResponse<object>.Error("404", "Cuenta bancaria no encontrada.");
            }

            cuenta.Activo = !cuenta.Activo;
            if (!cuenta.Activo)
            {
                cuenta.Principal = false;
            }
            cuenta.FechaActualizacion = DateTime.UtcNow;

            await _repository.ActualizarAsync(cuenta);

            return ApiResponse<object>.Success(null, "Estado actualizado.");
        }

        private static string? Validar(GuardarCuentaBancariaAdLocalDto d)
        {
            if (d == null) return "Datos de cuenta inválidos.";
            if (string.IsNullOrWhiteSpace(d.Banco)) return "El banco es requerido.";
            if (string.IsNullOrWhiteSpace(d.Beneficiario)) return "El beneficiario es requerido.";
            if (string.IsNullOrWhiteSpace(d.NumeroCuenta) && string.IsNullOrWhiteSpace(d.Clabe) && string.IsNullOrWhiteSpace(d.NumeroTarjeta))
                return "Captura una cuenta, CLABE o tarjeta.";
            return null;
        }

        private static string? Limpiar(string? v) => string.IsNullOrWhiteSpace(v) ? null : new string(v.Where(char.IsDigit).ToArray());

        private static CuentaBancariaAdLocalDto Mapear(CuentaBancariaAdLocal x) => new()
        {
            Uuid = x.Uuid,
            Banco = x.Banco,
            Beneficiario = x.Beneficiario,
            NumeroCuenta = x.NumeroCuenta,
            Clabe = x.Clabe,
            NumeroTarjeta = x.NumeroTarjeta,
            Instrucciones = x.Instrucciones,
            Principal = x.Principal,
            Activo = x.Activo
        };
    }
}
