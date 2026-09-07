using System;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Utils;

namespace AdLocalAPI.Services
{
    public partial class ComercioService
    {
        public async Task<ApiResponse<object>> guardarColaborador(ColaborarDto dto) 
        {
            long idUser = _jwtContext.GetUserId();
            var plan = await _planRepository.GetByTipoAsync(_jwtContext.GetPlanTipo());
            if (plan != null && plan.IsMultiUsuario)
            {
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                    return ApiResponse<object>.Error(
                        "400",
                        "El nombre del colaborador es obligatorio"
                    );
                if (string.IsNullOrWhiteSpace(dto.Correo))
                    return ApiResponse<object>.Error(
                        "400",
                        "El correo del colaborador es obligatorio"
                    );
                if (dto.IdComercio == 0)
                    return ApiResponse<object>.Error(
                        "400",
                        "El comercio es obligatorio"
                    );
                bool existente = await _usuarioRepository.ExistePorCorreoAsync(dto.Correo);

                if (existente)
                    return ApiResponse<object>.Error("400", "El correo ya está registrado");

                string codigo = ServicesGenerals.GenerarCodigoAlfanumerico(6);
                string token = Guid.NewGuid().ToString();
                var userColaborador = new Usuario
                {
                    Nombre = dto.Nombre,
                    Email = dto.Correo,
                    ComercioId = dto.IdComercio,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                    Codigo = codigo,
                    Token = token,
                    Rol = "Colaborador",
                    PasswordHash = "",
                };

                await _usuarioRepository.CreateAsync(userColaborador);
                bool esProduccion = _env.IsProduction();
                var link = UrlHelper.GenerarLinkNuevoColaborador(token, esProduccion);

                var html = TemplatesEmail.PlantillaCorreoBienvenidaColaborador(dto.Nombre,dto.Correo,codigo, link);

                await _emailService.EnviarCorreoAsync(
                    dto.Correo,
                    "¡Bienvenido a AdLocal! Crea tu contraseña",
                    html
                );
                return ApiResponse<object>.Success(
                    null,
                    "Colaborador creado correctamente"
                );
            }
            return ApiResponse<object>.Error("900", "No tienes permiso para agregar colaborasodree");
        }

        public async Task<ApiResponse<PagedResponse<UsuarioInfoDto>>> getAllColaboradores(long idComercio, int page = 1, int pageSize = 10)
        {
            try
            {
                return await _repository.getAllColaboradores(
                    idComercio,
                    page,
                    pageSize
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResponse<UsuarioInfoDto>>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> toggleAccesoColaborador(int idColaborador, long idComercio)
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                var user = await _usuarioRepository.GetByIdComercioAndIdUserAsync(idColaborador, idComercio);
                if (user == null)
                {
                    return ApiResponse<bool>.Error("404", "Colaborador no encontrado");
                }

                user.Activo = !user.Activo;
                await _usuarioRepository.UpdateAsync(user);

                if (!user.Activo)
                {
                    await _refreshTokenService.RevocarTodosPorUsuarioAsync(user.Id, "Acceso de colaborador desactivado");
                }

                return ApiResponse<bool>.Success(true, user.Activo ? "Activado correctamente" : "Desactivado correctamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> eliminarColaborador(int idColaborador, long idComercio)
        {
            try
            {
                long idUser = _jwtContext.GetUserId();
                var user = await _usuarioRepository.GetByIdComercioAndIdUserAsync(idColaborador, idComercio);
                if (user == null)
                {
                    return ApiResponse<bool>.Error("404", "Colaborador no encontrado");
                }

                if (!string.IsNullOrEmpty(user.FotoUrl))
                {
                    await _usuarioRepository.DeleteFromS3Async(user.FotoUrl);
                }
                await _usuarioRepository.DeleteColaboradorAsync(idColaborador,idComercio);
                await _refreshTokenService.RevocarTodosPorUsuarioAsync(user.Id, "Colaborador eliminado");

                return ApiResponse<bool>.Success(true, "Colaborador eliminado correctamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Error("500", ex.Message);
            }
        }
    }
}
