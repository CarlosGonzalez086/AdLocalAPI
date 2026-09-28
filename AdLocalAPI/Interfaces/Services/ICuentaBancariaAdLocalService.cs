using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Interfaces.Services
{
    public interface ICuentaBancariaAdLocalService
    {
        Task<ApiResponse<List<CuentaBancariaAdLocalDto>>> ListarAsync();
        Task<ApiResponse<CuentaBancariaAdLocalDto>> ObtenerPrincipalAsync();
        Task<ApiResponse<CuentaBancariaAdLocalDto>> CrearAsync(GuardarCuentaBancariaAdLocalDto dto);
        Task<ApiResponse<CuentaBancariaAdLocalDto>> ActualizarAsync(Guid uuid, GuardarCuentaBancariaAdLocalDto dto);
        Task<ApiResponse<object>> CambiarEstadoAsync(Guid uuid);
    }
}
