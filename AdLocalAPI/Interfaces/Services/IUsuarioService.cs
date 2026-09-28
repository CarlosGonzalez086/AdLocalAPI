using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<ApiResponse<PagedResponse<Usuario>>> GetAllUsuarios(int page, int pageSize, string orderBy, string search);
        Task<ApiResponse<object>> GetUsuarioById(int id);
        Task<ApiResponse<object>> DeleteUsuario(int id);
        Task<ApiResponse<object>> CrearUsuarioCliente(UsuarioRegistroDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<object>> CrearAdmin(AdminCreateDto dto);
        Task<ApiResponse<object>> ActualizarUsuario(UsuarioUpdateDto dto);
        Task<ApiResponse<object>> Login(string email, string password, CancellationToken cancellationToken = default);
        Task<string> GenerateJwtToken(Usuario usuario);
        Task<UpdateJwtResult> ActualizarJwtAsync(string email, bool updateJWT);
        Task<ApiResponse<object>> CambiarPassword(ChangePasswordDto dto);
        Task<ApiResponse<string>> UploadPhotoAsync(UploadPhotoDto dto);
        Task<ApiResponse<object>> ForgetPassword(string email);
        Task<ApiResponse<object>> NewPassword(NewPasswordDto dto);
        Task<ApiResponse<object>> CheckToken(string token);
        Task<ApiResponse<UsuarioInfoDto>> ObtenerInfoUsuario();
        Task<ApiResponse<object>> RenovarTokenAsync(RenovarTokenDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<object>> CambiarEstadoUsuario(long id, bool activo, string? motivo = null);
        Task<ApiResponse<object>> CambiarRolUsuario(long id, string nuevoRol);
        Task<ApiResponse<object>> VerificarCorreoAsync(VerificarCorreoDto dto);
        Task<ApiResponse<object>> ReenviarVerificacionAsync(ReenviarVerificacionDto dto);
    }
}
