using AdLocalAPI.Data;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class DireccionUsuarioRepository
        : IDireccionUsuarioRepository
    {
        private readonly AppDbContext _context;

        public DireccionUsuarioRepository(
            AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // OBTENER TODAS
        // ============================================================

        public async Task<List<DireccionUsuario>> ObtenerTodasAsync(
            long idUsuario)
        {
            return await _context.DireccionesUsuarios
                .AsNoTracking()
                .Include(x => x.Estado)
                .Include(x => x.Municipio)
                .Where(x =>
                    x.IdUsuario == idUsuario &&
                    !x.Eliminado
                )
                .OrderByDescending(x => x.EsPredeterminada)
                .ThenByDescending(x => x.FechaCreacion)
                .ToListAsync();
        }

        // ============================================================
        // OBTENER POR UUID
        // ============================================================

        public async Task<DireccionUsuario?> ObtenerPorUuidAsync(
            long idUsuario,
            Guid uuid)
        {
            return await _context.DireccionesUsuarios
                .Include(x => x.Estado)
                .Include(x => x.Municipio)
                .FirstOrDefaultAsync(x =>
                    x.IdUsuario == idUsuario &&
                    x.Uuid == uuid &&
                    !x.Eliminado
                );
        }

        // ============================================================
        // EXISTE ESTADO
        // ============================================================

        public async Task<bool> ExisteEstadoAsync(
            int idEstado)
        {
            return await _context.Estados
                .AnyAsync(x =>
                    x.Id == idEstado
                );
        }

        // ============================================================
        // EXISTE MUNICIPIO
        // ============================================================

        public async Task<bool> ExisteMunicipioAsync(
            int idMunicipio)
        {
            return await _context.Municipios
                .AnyAsync(x =>
                    x.Id == idMunicipio
                );
        }

        // ============================================================
        // CREAR
        // ============================================================

        public async Task<DireccionUsuario> CrearAsync(
            DireccionUsuario direccion)
        {
            await _context.DireccionesUsuarios
                .AddAsync(direccion);

            await _context.SaveChangesAsync();

            return direccion;
        }

        // ============================================================
        // ACTUALIZAR
        // ============================================================

        public async Task ActualizarAsync(
            DireccionUsuario direccion)
        {
            _context.DireccionesUsuarios
                .Update(direccion);

            await _context.SaveChangesAsync();
        }

        // ============================================================
        // QUITAR DIRECCIÓN PREDETERMINADA
        // ============================================================

        public async Task QuitarPredeterminadasAsync(
            long idUsuario,
            long? exceptoId = null)
        {
            var query =
                _context.DireccionesUsuarios
                    .Where(x =>
                        x.IdUsuario == idUsuario &&
                        x.EsPredeterminada &&
                        !x.Eliminado
                    );

            if (exceptoId.HasValue)
            {
                query = query.Where(x =>
                    x.Id != exceptoId.Value
                );
            }

            var direcciones =
                await query.ToListAsync();

            if (direcciones.Count == 0)
            {
                return;
            }

            foreach (var direccion in direcciones)
            {
                direccion.EsPredeterminada = false;
                direccion.FechaActualizacion =
                    DateTime.UtcNow;
            }

            _context.DireccionesUsuarios
                .UpdateRange(direcciones);

            await _context.SaveChangesAsync();
        }

        // ============================================================
        // OBTENER OTRA DIRECCIÓN ACTIVA
        // ============================================================

        public async Task<DireccionUsuario?> ObtenerPrimeraActivaAsync(
            long idUsuario,
            long? exceptoId = null)
        {
            var query =
                _context.DireccionesUsuarios
                    .Where(x =>
                        x.IdUsuario == idUsuario &&
                        x.Activo &&
                        !x.Eliminado
                    );

            if (exceptoId.HasValue)
            {
                query = query.Where(x =>
                    x.Id != exceptoId.Value
                );
            }

            return await query
                .OrderByDescending(x => x.FechaCreacion)
                .FirstOrDefaultAsync();
        }

        // ============================================================
        // SABER SI TIENE DIRECCIONES
        // ============================================================

        public async Task<bool> TieneDireccionesActivasAsync(
            long idUsuario)
        {
            return await _context.DireccionesUsuarios
                .AnyAsync(x =>
                    x.IdUsuario == idUsuario &&
                    x.Activo &&
                    !x.Eliminado
                );
        }
    }
}