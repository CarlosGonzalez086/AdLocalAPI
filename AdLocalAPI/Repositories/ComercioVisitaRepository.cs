using AdLocalAPI.Data;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;


using static AdLocalAPI.DTOs.ComercioVisitaDTOs;

namespace AdLocalAPI.Repositories
{
    public class ComercioVisitaRepository
    {
        private readonly AppDbContext _context;

        public ComercioVisitaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> RegistrarVisitaUnica(long comercioId, string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;

            var hoy = DateTime.UtcNow.Date;

            bool yaExiste = await _context.ComercioVisitas.AnyAsync(v =>
                v.ComercioId == comercioId &&
                v.Ip == ip &&
                v.FechaVisita.Date == hoy
            );

            if (yaExiste)
                return false;

            _context.ComercioVisitas.Add(new ComercioVisita
            {
                ComercioId = comercioId,
                Ip = ip,
                FechaVisita = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<ComercioVisitasStatsDto> GetStats(long comercioId)
        {
            var culture = new CultureInfo("es-ES");

            var tzMx = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

            var hoyMexico = TimeZoneInfo
                .ConvertTimeFromUtc(DateTime.UtcNow, tzMx)
                .Date;

            var inicioSemanaMexico = hoyMexico.AddDays(-6);
            var finSemanaMexico = hoyMexico.AddDays(1);

            var inicioSemanaUtc = TimeZoneInfo.ConvertTimeToUtc(inicioSemanaMexico, tzMx);
            var finSemanaUtc = TimeZoneInfo.ConvertTimeToUtc(finSemanaMexico, tzMx);

            var visitasSemanaDb = await _context.ComercioVisitas
                .Where(v =>
                    v.ComercioId == comercioId &&
                    v.FechaVisita >= inicioSemanaUtc &&
                    v.FechaVisita < finSemanaUtc
                )
                .AsNoTracking()
                .ToListAsync();

            var semana = Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var fechaLocal = inicioSemanaMexico.AddDays(i);

                    var total = visitasSemanaDb.Count(v =>
                        TimeZoneInfo.ConvertTimeFromUtc(v.FechaVisita, tzMx).Date == fechaLocal
                    );

                    return new VisitasPorDiaDto
                    {
                        Fecha = fechaLocal,
                        Total = total,
                        Dia = fechaLocal.ToString("dddd", culture)
                    };
                })
                .ToList();

            var inicioMesActualMexico = new DateTime(
                hoyMexico.Year,
                hoyMexico.Month,
                1
            );

            var inicioMesesMexico = inicioMesActualMexico.AddMonths(-2);
            var finMesMexico = inicioMesActualMexico.AddMonths(1);

            var inicioMesesUtc = TimeZoneInfo.ConvertTimeToUtc(inicioMesesMexico, tzMx);
            var finMesUtc = TimeZoneInfo.ConvertTimeToUtc(finMesMexico, tzMx);

            var visitasMesesDb = await _context.ComercioVisitas
                .Where(v =>
                    v.ComercioId == comercioId &&
                    v.FechaVisita >= inicioMesesUtc &&
                    v.FechaVisita < finMesUtc
                )
                .AsNoTracking()
                .ToListAsync();

            var meses = Enumerable.Range(0, 3)
                .Select(i =>
                {
                    var fechaLocal = inicioMesesMexico.AddMonths(i);

                    var total = visitasMesesDb.Count(v =>
                    {
                        var fechaMx = TimeZoneInfo.ConvertTimeFromUtc(v.FechaVisita, tzMx);
                        return fechaMx.Year == fechaLocal.Year &&
                               fechaMx.Month == fechaLocal.Month;
                    });

                    return new VisitasPorMesDto
                    {
                        Year = fechaLocal.Year,
                        Month = fechaLocal.Month,
                        Total = total,
                        Mes = fechaLocal.ToString("MMMM", culture)
                    };
                })
                .ToList();

            return new ComercioVisitasStatsDto
            {
                UltimaSemana = semana,
                UltimosTresMeses = meses
            };
        }





    }
}
