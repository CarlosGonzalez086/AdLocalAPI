using AdLocalAPI.DTOs;
using AdLocalAPI.Models;

namespace AdLocalAPI.Services.Interfaces
{
    public interface ICitaService
    {
        Task<ApiResponse<List<string>>> HorariosAsync(
            Guid productoUuid,
            DateOnly fecha
        );

        Task<ApiResponse<CitaDto>> CrearAsync(
            CrearCitaDto dto
        );

        Task<ApiResponse<List<CitaDto>>> MisCitasAsync();

        Task<ApiResponse<CitaDto>> CancelarClienteAsync(
            Guid uuid,
            string? motivo
        );

        Task<ApiResponse<CitaDto>> ReprogramarClienteAsync(
            Guid uuid,
            ReprogramarCitaDto dto
        );

        Task<ApiResponse<List<CitaDto>>> AgendaAsync(
            long comercioId,
            DateOnly? fecha
        );

        Task<ApiResponse<CitaDto>> ActualizarAsync(
            long comercioId,
            Guid uuid,
            ActualizarCitaComercioDto dto
        );
    }
}