using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;

namespace AdLocalAPI.Services
{
    public class PlanService
    {
        private readonly PlanRepository _repository;

        public PlanService(PlanRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<object>> GetAllPlanes(
            int page,
            int pageSize,
            string orderBy,
            string search
        )
        {
            try
            {
                var result = await _repository.GetAllAsync(page, pageSize, orderBy, search);

                return ApiResponse<object>.Success(
                    result,
                    "Listado de planes obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<object>> GetPlanById(int id)
        {
            try
            {
                var plan = await _repository.GetByIdAsync(id);

                if (plan == null)
                    return ApiResponse<object>.Error("404", "Plan no encontrado");

                return ApiResponse<object>.Success(
                    plan,
                    "Plan obtenido correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
        public async Task<ApiResponse<object>> CrearPlan(PlanCreateDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Nombre))
                    return ApiResponse<object>.Error("400", "El nombre del plan es obligatorio");

                if (dto.Precio <= 0)
                    return ApiResponse<object>.Error("400", "El precio debe ser mayor a cero");

                if (dto.DuracionDias <= 0)
                    return ApiResponse<object>.Error("400", "La duración debe ser mayor a cero días");

                if (string.IsNullOrEmpty(dto.Tipo))
                    return ApiResponse<object>.Error("400", "El tipo de plan es obligatorio");

                var plan = new Plan
                {
                    Nombre = dto.Nombre,
                    Precio = dto.Precio,
                    DuracionDias = dto.DuracionDias,
                    Tipo = dto.Tipo,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var creado = await _repository.CreateAsync(plan);

                return ApiResponse<object>.Success(
                    creado,
                    "Plan creado correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<object>> ActualizarPlan(int id, PlanCreateDto dto)
        {
            try
            {
                var plan = await _repository.GetByIdAsync(id);

                if (plan == null)
                    return ApiResponse<object>.Error("404", "Plan no encontrado");

                plan.Nombre = dto.Nombre;
                plan.Precio = dto.Precio;
                plan.DuracionDias = dto.DuracionDias;
                plan.Tipo = dto.Tipo;

                await _repository.UpdateAsync(plan);

                return ApiResponse<object>.Success(
                    plan,
                    "Plan actualizado correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }

        public async Task<ApiResponse<object>> EliminarPlan(int id)
        {
            try
            {
                var plan = await _repository.GetByIdAsync(id);

                if (plan == null)
                    return ApiResponse<object>.Error("404", "Plan no encontrado");

                await _repository.DeleteAsync(id);

                return ApiResponse<object>.Success(
                    null,
                    "Plan eliminado correctamente"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Error("500", ex.Message);
            }
        }
    }
}
