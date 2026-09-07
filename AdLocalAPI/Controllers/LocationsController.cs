using System.Threading.Tasks;
using AdLocalAPI.Interfaces.Location;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationsController : ApiControllerBase
    {
        private readonly ILocationService _service;

        public LocationsController(ILocationService service)
        {
            _service = service;
        }

        [HttpGet("states")]
        public async Task<IActionResult> GetAllStates()
        {
            var response = await _service.GetAllStatesAsync();
            return Responder(response);
        }

        [HttpGet("states/{id}/municipalities")]
        public async Task<IActionResult> GetMunicipalitiesByStateId(int id)
        {
            var response = await _service.GetMunicipalitiesByStateIdAsync(id);
            return Responder(response);
        }
    }
}
