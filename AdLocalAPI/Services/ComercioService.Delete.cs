using System;
using System.Threading.Tasks;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services
{
    public partial class ComercioService
    {
        public async Task<ApiResponse<object>> DeleteComercio(long id)
        {
            try
            {
                if (!_jwtContext.EstaAutenticado())
                    return ApiResponse<object>.Error("401", "No autenticado");

                if (!_jwtContext.EsComercio())
                    return ApiResponse<object>.Error("403", "No tienes permiso para eliminar este comercio");

                var comercio = await _repository.GetByIdAsync(id);

                if (comercio == null)
                    return ApiResponse<object>.Error(
                        "404",
                        "Comercio no encontrado"
                    );

                long userId = _jwtContext.GetUserId();
                if (comercio.IdUsuario != userId)
                {
                    return ApiResponse<object>.Error(
                        "403",
                        "No tienes permiso para eliminar este comercio"
                    );
                }

                if (!string.IsNullOrWhiteSpace(comercio.LogoUrl))
                {
                    await _repository.DeleteFromS3Async(comercio.LogoUrl);
                }

                var listaImagenes = await _comercioImagenRepositorio.ObtenerPorComercio(comercio.Id);
                if (listaImagenes.Count > 0)
                {
                    foreach (var img in listaImagenes)
                    {
                        await _comercioImagenRepositorio
                            .Eliminar(comercio.Id, img.FotoUrl);
                    }
                }

                bool siTiene = await _horarioComercioRepository.ComercioTieneHorariosAsync(comercio.Id);
                if (siTiene)
                {
                    await _horarioComercioRepository.EliminarHorariosPorComercioAsync(comercio.Id);
                }

                await _repository.DeleteAsync(id);

                return ApiResponse<object>.Success(
                    null,
                    "El comercio se eliminó correctamente"
                );
            }
            catch
            {
                return ApiResponse<object>.Error(
                    "500",
                    "Ocurrió un error al eliminar el comercio"
                );
            }
        }
    }
}
