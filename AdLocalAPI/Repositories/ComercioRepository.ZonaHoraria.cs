using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories;

public partial class ComercioRepository
{
    public async Task<string> ActualizarZonaHorariaAsync(long id, long usuario, ComercioZonaHorariaDto dto, CancellationToken ct = default)
    {
        CitaOperacion.Zona(dto.ZonaHoraria);
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        var comercio = await _context.Comercios.FromSqlInterpolated($"SELECT * FROM \"Comercios\" WHERE \"Id\" = {id} AND \"IdUsuario\" = {usuario} FOR UPDATE")
            .AsNoTracking().SingleOrDefaultAsync(ct);
        if (comercio == null) throw new CitaOperacionException("404", "Comercio no encontrado.");
        if (comercio.ZonaHoraria == dto.ZonaHoraria) { await tx.CommitAsync(ct); return comercio.ZonaHoraria; }
        CitaOperacion.Exigir(comercio.ZonaHoraria == dto.ZonaHorariaAnterior, "La zona cambió. Actualiza el comercio antes de continuar.");
        CitaOperacion.Exigir(!await _context.Citas.AnyAsync(x => x.IdComercio == id &&
            (x.Estado == EstadoCita.Pendiente || x.Estado == EstadoCita.Confirmada || x.Estado == EstadoCita.EnAtencion), ct),
            "Hay citas activas. Resuélvelas antes de cambiar la zona; no se desplazarán automáticamente.");
        await _context.Comercios.Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.ZonaHoraria, dto.ZonaHoraria)
            .SetProperty(x => x.FechaActualizacion, DateTime.UtcNow), ct);
        await tx.CommitAsync(ct); return dto.ZonaHoraria;
    }
}
