using AdLocalAPI.Data;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdLocalAPI.Controllers
{
    [ApiController, Route("api/CuentasBancariasAdLocal")]
    public class CuentasBancariasAdLocalController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CuentasBancariasAdLocalController(AppDbContext context) => _context = context;

        [Authorize(Roles = "Admin"), HttpGet]
        public async Task<IActionResult> Listar()
        {
            var cuentas = await _context.CuentasBancariasAdLocal.AsNoTracking().OrderByDescending(x => x.Principal).ThenBy(x => x.Banco).ToListAsync();
            return Ok(ApiResponse<List<CuentaBancariaAdLocalDto>>.Success(cuentas.Select(Mapear).ToList()));
        }

        [Authorize(Roles = "Comercio"), HttpGet("principal")]
        public async Task<IActionResult> Principal()
        {
            var cuenta = await _context.CuentasBancariasAdLocal.AsNoTracking().Where(x => x.Activo)
                .OrderByDescending(x => x.Principal).ThenByDescending(x => x.FechaCreacion).FirstOrDefaultAsync();
            return cuenta == null ? NotFound(ApiResponse<CuentaBancariaAdLocalDto>.Error("404", "ADLocal todavía no ha configurado una cuenta bancaria."))
                : Ok(ApiResponse<CuentaBancariaAdLocalDto>.Success(Mapear(cuenta)));
        }

        [Authorize(Roles = "Admin"), HttpPost]
        public async Task<IActionResult> Crear([FromBody] GuardarCuentaBancariaAdLocalDto dto)
        {
            var error = Validar(dto); if (error != null) return BadRequest(ApiResponse<object>.Error("400", error));
            if (dto.Principal) await QuitarPrincipalAsync();
            var cuenta = new CuentaBancariaAdLocal { Banco = dto.Banco.Trim(), Beneficiario = dto.Beneficiario.Trim(), NumeroCuenta = Limpiar(dto.NumeroCuenta), Clabe = Limpiar(dto.Clabe), NumeroTarjeta = Limpiar(dto.NumeroTarjeta), Instrucciones = dto.Instrucciones?.Trim(), Principal = dto.Principal, Activo = true };
            _context.CuentasBancariasAdLocal.Add(cuenta); await _context.SaveChangesAsync();
            return Ok(ApiResponse<CuentaBancariaAdLocalDto>.Success(Mapear(cuenta), "Cuenta registrada."));
        }

        [Authorize(Roles = "Admin"), HttpPut("{uuid:guid}")]
        public async Task<IActionResult> Actualizar(Guid uuid, [FromBody] GuardarCuentaBancariaAdLocalDto dto)
        {
            var cuenta = await _context.CuentasBancariasAdLocal.FirstOrDefaultAsync(x => x.Uuid == uuid); if (cuenta == null) return NotFound();
            var error = Validar(dto); if (error != null) return BadRequest(ApiResponse<object>.Error("400", error));
            if (dto.Principal) await QuitarPrincipalAsync(cuenta.Id);
            cuenta.Banco = dto.Banco.Trim(); cuenta.Beneficiario = dto.Beneficiario.Trim(); cuenta.NumeroCuenta = Limpiar(dto.NumeroCuenta); cuenta.Clabe = Limpiar(dto.Clabe); cuenta.NumeroTarjeta = Limpiar(dto.NumeroTarjeta); cuenta.Instrucciones = dto.Instrucciones?.Trim(); cuenta.Principal = dto.Principal; cuenta.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync(); return Ok(ApiResponse<CuentaBancariaAdLocalDto>.Success(Mapear(cuenta), "Cuenta actualizada."));
        }

        [Authorize(Roles = "Admin"), HttpPut("{uuid:guid}/estado")]
        public async Task<IActionResult> Estado(Guid uuid)
        {
            var cuenta = await _context.CuentasBancariasAdLocal.FirstOrDefaultAsync(x => x.Uuid == uuid); if (cuenta == null) return NotFound();
            cuenta.Activo = !cuenta.Activo; if (!cuenta.Activo) cuenta.Principal = false; cuenta.FechaActualizacion = DateTime.UtcNow; await _context.SaveChangesAsync(); return Ok(ApiResponse<object>.Success(null, "Estado actualizado."));
        }

        private async Task QuitarPrincipalAsync(long excepto = 0) { var actuales = await _context.CuentasBancariasAdLocal.Where(x => x.Principal && x.Id != excepto).ToListAsync(); foreach (var x in actuales) x.Principal = false; }
        private static string? Validar(GuardarCuentaBancariaAdLocalDto d) => string.IsNullOrWhiteSpace(d.Banco) ? "El banco es requerido." : string.IsNullOrWhiteSpace(d.Beneficiario) ? "El beneficiario es requerido." : string.IsNullOrWhiteSpace(d.NumeroCuenta) && string.IsNullOrWhiteSpace(d.Clabe) && string.IsNullOrWhiteSpace(d.NumeroTarjeta) ? "Captura una cuenta, CLABE o tarjeta." : null;
        private static string? Limpiar(string? v) => string.IsNullOrWhiteSpace(v) ? null : new string(v.Where(char.IsDigit).ToArray());
        private static CuentaBancariaAdLocalDto Mapear(CuentaBancariaAdLocal x) => new() { Uuid = x.Uuid, Banco = x.Banco, Beneficiario = x.Beneficiario, NumeroCuenta = x.NumeroCuenta, Clabe = x.Clabe, NumeroTarjeta = x.NumeroTarjeta, Instrucciones = x.Instrucciones, Principal = x.Principal, Activo = x.Activo };
    }
}
