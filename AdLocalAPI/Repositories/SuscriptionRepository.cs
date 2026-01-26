using AdLocalAPI.Data;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;

public interface ISuscriptionRepository
{
    Task<Suscripcion?> GetByIdAsync(int id);
    Task<List<Suscripcion>> GetByUsuarioAsync(int usuarioId);
    Task<Suscripcion> AddAsync(Suscripcion suscripcion);
    Task<Suscripcion> UpdateAsync(Suscripcion suscripcion);
}

public class SuscriptionRepository : ISuscriptionRepository
{
    private readonly AppDbContext _context;

    public SuscriptionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Suscripcion?> GetByIdAsync(int id)
    {
        return await _context.Suscripcions.Include(s => s.Plan).FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Suscripcion>> GetByUsuarioAsync(int usuarioId)
    {
        return await _context.Suscripcions.Include(s => s.Plan)
            .Where(s => s.UsuarioId == usuarioId && !s.Eliminada)
            .ToListAsync();
    }

    public async Task<Suscripcion> AddAsync(Suscripcion suscripcion)
    {
        _context.Suscripcions.Add(suscripcion);
        await _context.SaveChangesAsync();
        return suscripcion;
    }

    public async Task<Suscripcion> UpdateAsync(Suscripcion suscripcion)
    {
        _context.Suscripcions.Update(suscripcion);
        await _context.SaveChangesAsync();
        return suscripcion;
    }
}
