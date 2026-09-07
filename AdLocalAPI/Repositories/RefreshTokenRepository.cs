using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdLocalAPI.Data;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken> CrearAsync(RefreshToken token)
        {
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<RefreshToken?> ObtenerPorHashConUsuarioAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);
        }

        public async Task<RefreshToken?> ObtenerPorHashAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);
        }

        public async Task<RefreshToken?> ObtenerPorIdYUsuarioAsync(long sesionId, long usuarioId)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Id == sesionId && r.UsuarioId == usuarioId);
        }

        public async Task<List<RefreshToken>> ObtenerActivosPorUsuarioAsync(long usuarioId)
        {
            var ahora = DateTime.UtcNow;
            return await _context.RefreshTokens
                .AsNoTracking()
                .Where(r => r.UsuarioId == usuarioId && r.RevokedAt == null && r.ExpiresAt > ahora)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task ActualizarAsync(RefreshToken token)
        {
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        public async Task RotarTokenAsync(RefreshToken tokenActual, RefreshToken nuevoToken)
        {
            _context.RefreshTokens.Update(tokenActual);
            _context.RefreshTokens.Add(nuevoToken);
            await _context.SaveChangesAsync();
        }

        public async Task RevocarTodosPorUsuarioAsync(long usuarioId, string razon, string? ip)
        {
            var ahora = DateTime.UtcNow;
            var tokensActivos = await _context.RefreshTokens
                .Where(r => r.UsuarioId == usuarioId && r.RevokedAt == null && r.ExpiresAt > ahora)
                .ToListAsync();

            foreach (var t in tokensActivos)
            {
                t.RevokedAt = ahora;
                t.RevokedByIp = ip;
                t.ReasonRevoked = razon;
            }

            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario != null)
            {
                usuario.TokensRevocadosAntesDe = ahora;
            }

            await _context.SaveChangesAsync();
        }
    }
}
