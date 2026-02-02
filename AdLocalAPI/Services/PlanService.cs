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
        public async Task<ApiResponse<object>> GetAllPlanesUser()
        {
            try
            {
                var planes = await _repository.GetAllPlanesUser();

                return ApiResponse<object>.Success(
                    planes,
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
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                    return ApiResponse<object>.Error("400", "El nombre del plan es obligatorio");

                if (dto.DuracionDias <= 0)
                    return ApiResponse<object>.Error("400", "La duración debe ser mayor a cero");

                if (dto.Precio < 0)
                    return ApiResponse<object>.Error("400", "El precio no puede ser negativo");

                if (string.IsNullOrWhiteSpace(dto.Tipo))
                    return ApiResponse<object>.Error("400", "El tipo de plan es obligatorio");
                if (string.IsNullOrWhiteSpace(dto.StripePriceId))
                    return ApiResponse<object>.Error("400", "El tipo de plan es obligatorio");

                var plan = new Plan
                {
                    Nombre = dto.Nombre,
                    Precio = dto.Precio,
                    DuracionDias = dto.DuracionDias,
                    Tipo = dto.Tipo.ToUpper(),
                    StripePriceId = dto.StripePriceId,
                    MaxNegocios = dto.MaxNegocios,
                    MaxProductos = dto.MaxProductos,
                    MaxFotos = dto.MaxFotos,

                    NivelVisibilidad = dto.NivelVisibilidad,
                    PermiteCatalogo = dto.PermiteCatalogo,
                    ColoresPersonalizados = dto.ColoresPersonalizados,
                    TieneBadge = dto.TieneBadge,
                    BadgeTexto = dto.BadgeTexto,
                    TieneAnalytics = dto.TieneAnalytics,
                    IsMultiUsuario = dto.IsMultiUsuario,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var creado = await _repository.CreateAsync(plan);

                return ApiResponse<object>.Success(null, "Plan creado correctamente");
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
                plan.Tipo = dto.Tipo.ToUpper();
                plan.StripePriceId = dto.StripePriceId;
                plan.MaxNegocios = dto.MaxNegocios;
                plan.MaxProductos = dto.MaxProductos;
                plan.MaxFotos = dto.MaxFotos;
                plan.IsMultiUsuario = dto.IsMultiUsuario;
                plan.NivelVisibilidad = dto.NivelVisibilidad;
                plan.PermiteCatalogo = dto.PermiteCatalogo;
                plan.ColoresPersonalizados = dto.ColoresPersonalizados;
                plan.TieneBadge = dto.TieneBadge;
                plan.BadgeTexto = dto.BadgeTexto;
                plan.TieneAnalytics = dto.TieneAnalytics;

                await _repository.UpdateAsync(plan);

                return ApiResponse<object>.Success(null, "Plan actualizado correctamente"); ;
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
