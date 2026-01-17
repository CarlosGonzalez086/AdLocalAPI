using AdLocalAPI.Data;
using AdLocalAPI.Interfaces.Location;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class LocationRepository : ILocationRepository
    {
        private readonly AppDbContext _context;

        public LocationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Estado>> GetAllStatesAsync()
        {
            return await _context.Estados.ToListAsync();
        }

        public async Task<Estado> GetStateByIdAsync(int id)
        {
            return await _context.Estados.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Municipio>> GetMunicipalitiesByStateIdAsync(int stateId)
        {
            return await _context.EstadosMunicipios
                .Where(x => x.EstadoId == stateId)
                .Include(x => x.Municipio)
                .Select(x => x.Municipio)
                .ToListAsync();
        }

        public async Task<Municipio> GetMunicipalityByIdAsync(int id)
        {
            var relStateMunicipality = await _context.EstadosMunicipios.FirstOrDefaultAsync(x => x.MunicipioId == id);
            return await _context.Municipios.FirstOrDefaultAsync(x => x.Id == relStateMunicipality.MunicipioId);
        }
    }
}
