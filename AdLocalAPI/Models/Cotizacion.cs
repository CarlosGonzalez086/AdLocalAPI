using System.ComponentModel.DataAnnotations;using System.ComponentModel.DataAnnotations.Schema;
namespace AdLocalAPI.Models;
[Table("cotizaciones")] public class Cotizacion{[Key]public long Id{get;set;}public Guid Uuid{get;set;}=Guid.NewGuid();public long IdUsuario{get;set;}public long IdComercio{get;set;}public long IdProductoServicio{get;set;}[MaxLength(1000)]public string Solicitud{get;set;}=string.Empty;[MaxLength(1000)]public string? Respuesta{get;set;}public decimal? PrecioPropuesto{get;set;}public EstadoCotizacion Estado{get;set;}=EstadoCotizacion.Pendiente;public DateTime FechaCreacion{get;set;}=DateTime.UtcNow;public DateTime? FechaActualizacion{get;set;}}
public enum EstadoCotizacion{Pendiente=1,Respondida=2,Aceptada=3,Rechazada=4,Cancelada=5}
