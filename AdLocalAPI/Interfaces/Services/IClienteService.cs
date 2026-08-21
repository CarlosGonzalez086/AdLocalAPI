using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.DTOs.UsuarioCliente;

namespace AdLocalAPI.Services.Interfaces
{
    public interface IClienteService
    {
        Task<ApiResponse<object>> CrearCliente(ClienteRegistroDto dto);
        Task<ApiResponse<object>> LoginCliente(LoginDto dto);
        Task<ApiResponse<object>> EnviarCodigoRecuperacion(EmailDto dto);
        Task<ApiResponse<object>> VerificarCodigo(VerificarCodigoDto dto);
        Task<ApiResponse<object>> RestablecerPassword(RestablecerPasswordDto dto);
        Task<ApiResponse<PerfilClienteDto>> ObtenerPerfilAsync();
        Task<ApiResponse<PerfilClienteActualizadoDto>> ActualizarPerfilAsync(ActualizarPerfilClienteDto dto);
    }
}
