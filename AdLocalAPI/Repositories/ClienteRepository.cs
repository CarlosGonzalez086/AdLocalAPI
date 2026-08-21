using AdLocalAPI.Data;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly AppDbContext _context;

        public ClienteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ObtenerPorEmailAsync(string email)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());
        }

        public async Task<Usuario?> ObtenerPorIdAsync(long id)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> ExisteEmailAsync(string email)
        {
            return await _context.Usuarios.AnyAsync(x =>x.Email.ToLower() == email.ToLower());
        }

        public async Task<Usuario> CrearAsync(Usuario usuario)
        {
            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();

            return usuario;
        }

        public async Task ActualizarAsync(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
        }
    }
}