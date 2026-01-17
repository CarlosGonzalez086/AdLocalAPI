using AdLocalAPI.DTOs;
using AdLocalAPI.Interfaces.Location;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services
{
    public class LocationService : ILocationService
    {
        private readonly ILocationRepository _repository;

        public LocationService(ILocationRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<List<StateDto>>> GetAllStatesAsync()
        {
            try
            {
                var states = await _repository.GetAllStatesAsync();

                if (states == null || states.Count == 0)
                    return ApiResponse<List<StateDto>>.Error("404", "States not found");

                var dto = states.Select(x => new StateDto
                {
                    Id = x.Id,
                    Name = x.EstadoNombre
                }).ToList();

                return ApiResponse<List<StateDto>>.Success(dto, "States retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<StateDto>>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<List<MunicipalityDto>>> GetMunicipalitiesByStateIdAsync(int stateId)
        {
            try
            {
                var state = await _repository.GetStateByIdAsync(stateId);

                if (state == null)
                    return ApiResponse<List<MunicipalityDto>>.Error("404", "State not found");

                var municipalities = await _repository.GetMunicipalitiesByStateIdAsync(stateId);

                if (municipalities == null || municipalities.Count == 0)
                    return ApiResponse<List<MunicipalityDto>>.Error("404", "Municipalities not found");

                var dto = municipalities.Select(x => new MunicipalityDto
                {
                    Id = x.Id,
                    Name = x.MunicipioNombre
                }).ToList();

                return ApiResponse<List<MunicipalityDto>>.Success(
                    dto,
                    "Municipalities retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<List<MunicipalityDto>>.Error("500", ex.Message);
            }
        }
    }
}
