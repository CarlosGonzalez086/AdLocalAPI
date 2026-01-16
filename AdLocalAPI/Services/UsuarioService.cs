using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using AdLocalAPI.Utils;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AdLocalAPI.Services
{
    public class UsuarioService
    {
        private readonly UsuarioRepository _repository;
        private readonly IConfiguration _config;
        private readonly JwtContext _jwtContext;
        private readonly ComercioRepository _comercioRepository;
        private readonly EmailService _emailService;
        private readonly IWebHostEnvironment _env;

        public UsuarioService(UsuarioRepository repository, IConfiguration config, JwtContext jwtContext,ComercioRepository comercioRepository, EmailService emailService, IWebHostEnvironment env)
        {
            _repository = repository;
            _config = config;
            _jwtContext = jwtContext;
            _comercioRepository = comercioRepository;
            _emailService = emailService;
            _env = env;

        }

        public async Task<ApiResponse<object>> GetAllUsuarios(int page,
            int pageSize,
            string orderBy,
            string search)
        {
            try
            {
                var result = await _repository.GetAllAsync(page, pageSize, orderBy, search);

                return ApiResponse<object>.Success(
                    result,
                    "Listado de usuarios obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
        public async Task<ApiResponse<object>> GetUsuarioById(int id)
        {
            try
            {
                var usuario = await _repository.GetByIdAsync(id);

                if (usuario == null)
                    return ApiResponse<object>.Error("404", "Usuario no encontrado");

                return ApiResponse<object>.Success(
                    usuario,
                    "Usuario obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
        public async Task<ApiResponse<object>> CrearUsuarioCliente(UsuarioRegistroDto dto)
        {
            if (string.IsNullOrEmpty(dto.Nombre))
                return ApiResponse<object>.Error("400", "El nombre es obligatorio");

            if (string.IsNullOrEmpty(dto.Email))
                return ApiResponse<object>.Error("400", "El email es obligatorio");

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

            if (!emailRegex.IsMatch(dto.Email))
                return ApiResponse<object>.Error("400", "El correo electrónico no es válido");

            if (string.IsNullOrEmpty(dto.Password))
                return ApiResponse<object>.Error("400", "La contraseña es obligatoria");

            bool existente = await _repository.ExistePorCorreoAsync(dto.Email);

            if (existente)
                return ApiResponse<object>.Error("400", "El correo ya está registrado");

            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Email = dto.Email,
                Rol = "Comercio",
                ComercioId = dto.ComercioId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FechaCreacion = DateTime.UtcNow
            };

            var creado = await _repository.CreateAsync(usuario);

            return ApiResponse<object>.Success(
                new { creado.Id, creado.Nombre, creado.Email, creado.Rol },
                "Usuario creado correctamente"
            );
        }
        public async Task<ApiResponse<object>> CrearAdmin(AdminCreateDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Nombre))
                    return ApiResponse<object>.Error("400", "El nombre es obligatorio");

                if (string.IsNullOrEmpty(dto.Email))
                    return ApiResponse<object>.Error("400", "El email es obligatorio");

                if (string.IsNullOrEmpty(dto.Password))
                    return ApiResponse<object>.Error("400", "La contraseña es obligatoria");

                bool existente = await _repository.ExistePorCorreoAsync(dto.Email);

                if (existente)
                    return ApiResponse<object>.Error("400", "El correo ya está registrado");

                var admin = new Usuario
                {
                    Nombre = dto.Nombre,
                    Email = dto.Email,
                    Rol = "Admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    FechaCreacion = DateTime.UtcNow
                };

                var creado = await _repository.CreateAsync(admin);

                return ApiResponse<object>.Success(
                    new
                    {
                        creado.Id,
                        creado.Nombre,
                        creado.Email,
                        creado.Rol,
                        creado.FechaCreacion
                    },
                    "Administrador creado correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
        public async Task<ApiResponse<object>> ActualizarUsuario(UsuarioUpdateDto dto)
        {
            int id = _jwtContext.GetUserId();
            var usuario = await _repository.GetByIdAsync(id);

            if (usuario == null)
                return ApiResponse<object>.Error("404", "Usuario no encontrado");

            usuario.Nombre = dto.Nombre;
            usuario.Email = dto.Email;

            await _repository.UpdateAsync(usuario);

            return ApiResponse<object>.Success(null, "Usuario actualizado correctamente");
        }
        public async Task<ApiResponse<UsuarioInfoDto>> ObtenerInfoUsuario()
        {
            int id = _jwtContext.GetUserId();
            var usuario = await _repository.GetByIdAsync(id);

            if (usuario == null)
                return ApiResponse<UsuarioInfoDto>.Error("404", "Usuario no encontrado");

            var info = new UsuarioInfoDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol,
                ComercioId = usuario.ComercioId,
                FechaCreacion = usuario.FechaCreacion,
                Activo = usuario.Activo,
                FotoUrl = usuario.FotoUrl,
            };

            return ApiResponse<UsuarioInfoDto>.Success(info);
        }
        public async Task<ApiResponse<object>> DeleteUsuario(int id)
        {
            try
            {
                var usuario = await _repository.GetByIdAsync(id);

                if (usuario == null)
                    return ApiResponse<object>.Error("404", "Usuario no encontrado");

                await _repository.DeleteAsync(id);

                return ApiResponse<object>.Success(
                    null,
                    "Usuario eliminado correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
        public async Task<ApiResponse<object>> Login(string email, string password)
        {
            var usuario = await _repository.GetByCorreoAsync(email);

            if (usuario == null)
                return ApiResponse<object>.Error("401", "Credenciales inválidas");

            bool valid = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);

            if (!valid)
                return ApiResponse<object>.Error("401", "Credenciales inválidas");

            var token = await GenerateJwtToken(usuario);

            object respuesta;

            if (usuario.Rol == "Admin")
            {
                respuesta = new
                {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Email,
                    usuario.Rol,
                    Token = token
                };
            }
            else if (usuario.Rol == "Comercio")
            {
                respuesta = new
                {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Email,
                    usuario.Rol,
                    usuario.ComercioId,
                    Token = token
                };
            }
            else
            {
                return ApiResponse<object>.Error("403", "Rol no autorizado");
            }

            return ApiResponse<object>.Success(
                respuesta,
                "Inicio de sesión exitoso"
            );
        }
        public async Task<string> GenerateJwtToken(Usuario usuario)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Email),
                new Claim("id", usuario.Id.ToString()),
                new Claim("nombre", usuario.Nombre),
                new Claim("rol", usuario.Rol)
            };

            if (usuario.Rol == "Comercio")
            {
                Comercio comercio = await _comercioRepository.GetComercioByUser(usuario.Id);
                if (comercio != null)
                {
                    claims.Add(new Claim("comercioId", comercio.Id.ToString()));
                    claims.Add(new Claim("FotoUrl", usuario.FotoUrl == null ? "" : usuario.FotoUrl));
                }
                else 
                {
                    claims.Add(new Claim("comercioId", usuario.ComercioId == null ? "0" : usuario.ComercioId.Value.ToString()));
                    claims.Add(new Claim("FotoUrl", usuario.FotoUrl == null ? "" : usuario.FotoUrl));
                }
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<UpdateJwtResult> ActualizarJwtAsync(string email, bool updateJWT)
        {
            var usuario = await _repository.GetByCorreoAsync(email);

            if (usuario == null)
            {
                return new UpdateJwtResult
                {
                    Success = false,
                    Message = "Usuario no encontrado"
                };
            }

            string? token = null;

            if (updateJWT)
            {
                token =  await GenerateJwtToken(usuario);
            }

            return new UpdateJwtResult
            {
                Success = true,
                Message = updateJWT
                    ? "JWT actualizado correctamente"
                    : "JWT no actualizado",
                Token = token,
                Usuario = usuario
            };
        }
        public async Task<ApiResponse<object>> CambiarPassword(ChangePasswordDto dto)
        {
            int userId = _jwtContext.GetUserId();

            var usuario = await _repository.GetByIdAsync(userId);
            if (usuario == null)
                return ApiResponse<object>.Error("404", "Usuario no encontrado");


            bool passwordCorrecto = BCrypt.Net.BCrypt.Verify(
                dto.PasswordActual,
                usuario.PasswordHash
            );

            if (!passwordCorrecto)
                return ApiResponse<object>.Error(
                    "400",
                    "La contraseña actual es incorrecta"
                );

            if (dto.PasswordNueva.Length < 8)
                return ApiResponse<object>.Error(
                    "400",
                    "La nueva contraseña debe tener al menos 8 caracteres"
                );

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva);

            await _repository.UpdateAsync(usuario);

            return ApiResponse<object>.Success(
                null,
                "Contraseña actualizada correctamente"
            );
        }
        public async Task<ApiResponse<string>> UploadPhotoAsync(UploadPhotoDto dto)
        {
            int id = _jwtContext.GetUserId();
            var usuario = await _repository.GetByIdAsync(id);
            if (string.IsNullOrEmpty(dto.Base64))
                return ApiResponse<string>.Error("400", "No se recibió la imagen");

            var tiposPermitidos = new List<string> { "image/jpeg", "image/jpg", "image/png", "image/webp" };

            string base64Data = dto.Base64;
            string tipoImagen = null;

            if (base64Data.StartsWith("data:"))
            {
                var parts = base64Data.Split(',');
                if (parts.Length != 2)
                    return ApiResponse<string>.Error("400", "Formato de imagen inválido");

                tipoImagen = parts[0].Replace("data:", "").Replace(";base64", "");
                base64Data = parts[1];
            }

            if (tipoImagen != null && !tiposPermitidos.Contains(tipoImagen.ToLower()))
                return ApiResponse<string>.Error("400", "Tipo de imagen no permitido. Solo JPEG, JPG, PNG o WEBP");

            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                if (!string.IsNullOrWhiteSpace(usuario.FotoUrl))
                {
                    bool deleted = await _repository.DeleteFromSupabaseByUrlAsync(usuario.FotoUrl);

                    if (!deleted)
                    {
                        return ApiResponse<string>.Error(
                            "500",
                            "No fue posible eliminar la imagen anterior"
                        );
                    }
                }

                string contentType = tipoImagen ?? "image/png";

                string newUrl = await _repository.UploadToSupabaseAsync(
                    imageBytes,
                    id,
                    contentType
                );

                await _repository.UpdateUserPhotoUrlAsync(id, newUrl);

                return ApiResponse<string>.Success(
                    newUrl,
                    "La imagen se actualizó correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Error("500", ex.Message);
            }
        }
        public async Task<ApiResponse<object>> ForgetPassword(string email)
        {
            try
            {
                var usuario = await _repository.GetByCorreoAsync(email);

                if (usuario == null)
                    return ApiResponse<object>.Error("404", "Usuario no encontrado");

                string codigo = ServicesGenerals.GenerarCodigoAlfanumerico(6);
                string token = Guid.NewGuid().ToString();

                usuario.Codigo = codigo;
                usuario.Token = token;

                await _repository.UpdateAsync(usuario);

                bool esProduccion = _env.IsProduction();
                var link = UrlHelper.GenerarLinkCambioPassword(token, esProduccion);

                var html = TemplatesEmail.PlantillaCorreoCambioPasswordCoffee(codigo, link);

                await _emailService.EnviarCorreoAsync(
                    usuario.Email,
                    "Restablecer contraseña - AdLocal",
                    html
                );

                return ApiResponse<object>.Success(
                    null,
                    "Correo de recuperación enviado correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
        public async Task<ApiResponse<object>> NewPassword(NewPasswordDto dto)
        {
            var usuario = await _repository.GetByCodeAsync(dto.Codigo);
            if (usuario == null)
                return ApiResponse<object>.Error("404", "Usuario no encontrado");

            if (dto.PasswordNueva.Length < 8)
                return ApiResponse<object>.Error(
                    "400",
                    "La nueva contraseña debe tener al menos 8 caracteres"
                );
            usuario.Token = null;
            usuario.Codigo = null;
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva);

            await _repository.UpdateAsync(usuario);

            return ApiResponse<object>.Success(
                null,
                "Contraseña actualizada correctamente"
            );
        }
        public async Task<ApiResponse<object>> CheckToken(string token)
        {
            var usuario = await _repository.GetByTokenAsync(token);

            if (usuario == null)
            {
                return ApiResponse<object>.Error(
                    "404",
                    "El enlace de recuperación no es válido o ya ha expirado. Solicita uno nuevo."
                );
            }

            return ApiResponse<object>.Success(
                null,
                "El token es válido. Puedes continuar con el cambio de contraseña."
            );
        }


    }
}
