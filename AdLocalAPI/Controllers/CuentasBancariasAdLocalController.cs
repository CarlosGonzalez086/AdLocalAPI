using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/CuentasBancariasAdLocal")]
    public class CuentasBancariasAdLocalController : ApiControllerBase
    {
        private readonly ICuentaBancariaAdLocalService _cuentaBancariaService;

        public CuentasBancariasAdLocalController(ICuentaBancariaAdLocalService cuentaBancariaService)
        {
            _cuentaBancariaService = cuentaBancariaService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var response = await _cuentaBancariaService.ListarAsync();
            return Responder(response);
        }

        [Authorize(Roles = "Comercio")]
        [HttpGet("principal")]
        public async Task<IActionResult> Principal()
        {
            var response = await _cuentaBancariaService.ObtenerPrincipalAsync();
            return Responder(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] GuardarCuentaBancariaAdLocalDto dto)
        {
            var response = await _cuentaBancariaService.CrearAsync(dto);
            return Responder(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{uuid:guid}")]
        public async Task<IActionResult> Actualizar(Guid uuid, [FromBody] GuardarCuentaBancariaAdLocalDto dto)
        {
            var response = await _cuentaBancariaService.ActualizarAsync(uuid, dto);
            return Responder(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{uuid:guid}/estado")]
        public async Task<IActionResult> Estado(Guid uuid)
        {
            var response = await _cuentaBancariaService.CambiarEstadoAsync(uuid);
            return Responder(response);
        }
    }
}
