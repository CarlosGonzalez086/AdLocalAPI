namespace AdLocalAPI.DTOs
{
    public class CrearTarjetaDto
    {
        public string PaymentMethodId { get; set; } = null!;
        public bool IsDefault { get; set; }
    }
}
