using System.Threading;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.DTOs.UsuarioCliente;

namespace AdLocalAPI.Services.Interfaces
{
    public interface IClienteService
    {
        Task<ApiResponse<object>> CrearCliente(ClienteRegistroDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<object>> LoginCliente(LoginDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<object>> EnviarCodigoRecuperacion(EmailDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<object>> VerificarCodigo(VerificarCodigoDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<object>> RestablecerPassword(RestablecerPasswordDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<PerfilClienteDto>> ObtenerPerfilAsync(CancellationToken cancellationToken = default);
        Task<ApiResponse<PerfilClienteActualizadoDto>> ActualizarPerfilAsync(ActualizarPerfilClienteDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<object>> RenovarTokenAsync(RenovarTokenDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<object>> VerificarCorreoAsync(VerificarCorreoDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<object>> ReenviarVerificacionAsync(ReenviarVerificacionDto dto, CancellationToken cancellationToken = default);
    }
}
