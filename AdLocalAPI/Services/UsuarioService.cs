using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
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

        public UsuarioService(UsuarioRepository repository, IConfiguration config, JwtContext jwtContext,ComercioRepository comercioRepository)
        {
            _repository = repository;
            _config = config;
            _jwtContext = jwtContext;
            _comercioRepository = comercioRepository;
        }

        public async Task<ApiResponse<List<Usuario>>> GetAllUsuarios()
        {
            var usuarios = await _repository.GetAllAsync();
            return ApiResponse<List<Usuario>>.Success(usuarios);
        }

        public async Task<ApiResponse<Usuario>> GetUsuarioById(int id)
        {
            var usuario = await _repository.GetByIdAsync(id);

            if (usuario == null)
                return ApiResponse<Usuario>.Error("404", "Usuario no encontrado");

            return ApiResponse<Usuario>.Success(usuario);
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

            var existente = (await _repository.GetAllAsync())
                .FirstOrDefault(u => u.Email.ToLower() == dto.Email.ToLower());

            if (existente != null)
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

                var existente = (await _repository.GetAllAsync())
                    .FirstOrDefault(u => u.Email.ToLower() == dto.Email.ToLower());

                if (existente != null)
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

            var existente = (await _repository.GetAllAsync())
                .FirstOrDefault(u => u.Email.ToLower() == dto.Email.ToLower() && u.Id != id);

            if (existente != null)
                return ApiResponse<object>.Error("400", "El correo ya está registrado");

            usuario.Nombre = dto.Nombre;
            usuario.Email = dto.Email;

            if (!string.IsNullOrEmpty(dto.Password))
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            if (usuario.Rol == "Comercio")
                usuario.ComercioId = dto.ComercioId;

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
            await _repository.DeleteAsync(id);
            return ApiResponse<object>.Success(null, "Usuario eliminado correctamente");
        }

        public async Task<ApiResponse<object>> Login(string email, string password)
        {
            var usuario = (await _repository.GetAllAsync())
                .FirstOrDefault(u => u.Email == email);

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
                if (comercio.IdUsuario == usuario.Id)
                {
                    claims.Add(new Claim("comercioId", comercio.IdUsuario.ToString()));
                }
                else 
                {
                    claims.Add(new Claim("comercioId", usuario.ComercioId.Value.ToString()));
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
            var usuario = (await _repository.GetAllAsync())
                .FirstOrDefault(u => u.Email == email);

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

                // ✅ Pasar tipoImagen a UploadToSupabaseAsync
                string url = await _repository.UploadToSupabaseAsync(imageBytes, id, tipoImagen ?? "image/png");

                await _repository.UpdateUserPhotoUrlAsync(id, url);

                return ApiResponse<string>.Success(url, "Foto subida correctamente");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Error("500", ex.Message);
            }
        }




    }
}
