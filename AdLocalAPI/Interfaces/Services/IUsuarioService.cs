using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<ApiResponse<PagedResponse<Usuario>>> GetAllUsuarios(int page, int pageSize, string orderBy, string search);
        Task<ApiResponse<object>> GetUsuarioById(int id);
        Task<ApiResponse<object>> DeleteUsuario(int id);
        Task<ApiResponse<object>> CrearUsuarioCliente(UsuarioRegistroDto dto);
        Task<ApiResponse<object>> CrearAdmin(AdminCreateDto dto);
        Task<ApiResponse<object>> ActualizarUsuario(UsuarioUpdateDto dto);
        Task<ApiResponse<object>> Login(string email, string password);
        Task<string> GenerateJwtToken(Usuario usuario);
        Task<UpdateJwtResult> ActualizarJwtAsync(string email, bool updateJWT);
        Task<ApiResponse<object>> CambiarPassword(ChangePasswordDto dto);
        Task<ApiResponse<string>> UploadPhotoAsync(UploadPhotoDto dto);
        Task<ApiResponse<object>> ForgetPassword(string email);
        Task<ApiResponse<object>> NewPassword(NewPasswordDto dto);
        Task<ApiResponse<object>> CheckToken(string token);
        Task<ApiResponse<UsuarioInfoDto>> ObtenerInfoUsuario();
        Task<ApiResponse<object>> RenovarTokenAsync(RenovarTokenDto dto);
    }
}
