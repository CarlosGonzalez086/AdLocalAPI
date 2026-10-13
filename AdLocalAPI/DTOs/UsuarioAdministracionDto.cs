using AdLocalAPI.Models;

namespace AdLocalAPI.DTOs;

// Lista explícita de campos permitidos; nunca serializar credenciales ni navegaciones EF.
public sealed class UsuarioAdministracionDto
{
    public long Id { get; init; }
    public Guid Uuid { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;
    public bool Activo { get; init; }
    public bool EmailVerificado { get; init; }
    public string? Telefono { get; init; }
    public string? FotoUrl { get; init; }
    public long? ComercioId { get; init; }
    public string? CodigoReferido { get; init; }
    public bool RedeemMonthFree { get; init; }
    public bool RedeemRewards { get; init; }
    public DateTime FechaCreacion { get; init; }
    public DateTime? FechaActualizacion { get; init; }
    public DateTime? UltimoAcceso { get; init; }

    public static UsuarioAdministracionDto Desde(Usuario usuario) => new()
    {
        Id = usuario.Id, Uuid = usuario.Uuid, Nombre = usuario.Nombre,
        Email = usuario.Email, Rol = usuario.Rol, Activo = usuario.Activo,
        EmailVerificado = usuario.EmailVerificado, Telefono = usuario.Telefono,
        FotoUrl = usuario.FotoUrl, ComercioId = usuario.ComercioId,
        CodigoReferido = usuario.CodigoReferido, RedeemMonthFree = usuario.RedeemMonthFree,
        RedeemRewards = usuario.RedeemRewards, FechaCreacion = usuario.FechaCreacion,
        FechaActualizacion = usuario.FechaActualizacion, UltimoAcceso = usuario.UltimoAcceso
    };
}
