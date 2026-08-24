using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Amazon.S3.Util.S3EventNotification;

namespace AdLocalAPI.Repositories
{
    public class CitaRepository : ICitaRepository
    {
        private readonly AppDbContext _context;

        public CitaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Cita?> ObtenerPorIdAsync(long id)
        {
            return await _context.Citas
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Cita?> ObtenerPorUuidClienteAsync(
            Guid uuid,
            long usuarioId
        )
        {
            return await _context.Citas
                .FirstOrDefaultAsync(x =>
                    x.Uuid == uuid &&
                    x.IdUsuario == usuarioId
                );
        }

        public async Task<Cita?> ObtenerPorUuidComercioAsync(
            Guid uuid,
            long comercioId
        )
        {
            return await _context.Citas
                .FirstOrDefaultAsync(x =>
                    x.Uuid == uuid &&
                    x.IdComercio == comercioId
                );
        }

        public async Task<List<Cita>> ObtenerOcupadasAsync(
            long comercioId,
            DateTime inicio,
            DateTime fin
        )
        {
            return await _context.Citas
                .AsNoTracking()
                .Where(x =>
                    x.IdComercio == comercioId &&
                    x.Estado != EstadoCita.Cancelada &&
                    x.FechaInicio < fin &&
                    x.FechaFin > inicio
                )
                .ToListAsync();
        }

        public async Task<List<CitaDto>> ObtenerPorUsuarioAsync(
            long usuarioId
        )
        {
            return await Consulta()
                .Where(x => x.Cita.IdUsuario == usuarioId)
                .Select(x => Map(x))
                .ToListAsync();
        }

        public async Task<List<CitaDto>> ObtenerAgendaAsync(
            long comercioId,
            DateOnly? fecha
        )
        {
            var query = Consulta()
                .Where(x => x.Cita.IdComercio == comercioId);

            if (fecha.HasValue)
            {
                var desde = fecha.Value.ToDateTime(TimeOnly.MinValue);
                var hasta = desde.AddDays(1);

                query = query.Where(x =>
                    x.Cita.FechaInicio >= desde &&
                    x.Cita.FechaInicio < hasta
                );
            }

            return await query
                .OrderBy(x => x.Cita.FechaInicio)
                .Select(x => Map(x))
                .ToListAsync();
        }

        public async Task<CitaDto?> ObtenerDtoAsync(long id)
        {
            return await Consulta()
                .Where(x => x.Cita.Id == id)
                .Select(x => Map(x))
                .FirstOrDefaultAsync();
        }

        public async Task<Cita> CrearAsync(Cita cita)
        {
            _context.Citas.Add(cita);

            await _context.SaveChangesAsync();

            return cita;
        }

        public async Task GuardarCambiosAsync(Cita cita)
        {
            _context.Citas.Update(cita);
            await _context.SaveChangesAsync();
        }

        private IQueryable<CitaConsulta> Consulta()
        {
            return
                from cita in _context.Citas.AsNoTracking()

                join comercio in _context.Comercios.AsNoTracking()
                    on cita.IdComercio equals comercio.Id

                join servicio in _context.ProductosServicios.AsNoTracking()
                    on cita.IdProductoServicio equals servicio.Id

                join usuario in _context.Usuarios.AsNoTracking()
                    on cita.IdUsuario equals usuario.Id

                select new CitaConsulta
                {
                    Cita = cita,
                    Comercio = comercio,
                    Servicio = servicio,
                    Usuario = usuario
                };
        }

        private static CitaDto Map(CitaConsulta x)
        {
            return new CitaDto
            {
                Uuid = x.Cita.Uuid,

                ProductoUuid = x.Servicio.Uuid,

                Comercio = x.Comercio.Nombre,

                Servicio = x.Servicio.Nombre,

                Cliente = x.Usuario.Nombre,

                TelefonoCliente = x.Usuario.Telefono,

                NombrePersona = x.Cita.NombrePersona,

                NotasCliente = x.Cita.NotasCliente,

                NombreAtiende = x.Cita.NombreAtiende,

                FechaInicio = x.Cita.FechaInicio,

                FechaFin = x.Cita.FechaFin,

                Estado = x.Cita.Estado,

                MotivoCancelacion = x.Cita.MotivoCancelacion
            };
        }

        private sealed class CitaConsulta
        {
            public Cita Cita { get; set; } = null!;

            public Comercio Comercio { get; set; } = null!;

            public ProductosServicios Servicio { get; set; } = null!;

            public Usuario Usuario { get; set; } = null!;
        }
    }
}