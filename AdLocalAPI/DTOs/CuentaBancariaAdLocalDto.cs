namespace AdLocalAPI.DTOs
{
    public class CuentaBancariaAdLocalDto
    {
        public Guid Uuid { get; set; }
        public string Banco { get; set; } = string.Empty;
        public string Beneficiario { get; set; } = string.Empty;
        public string? NumeroCuenta { get; set; }
        public string? Clabe { get; set; }
        public string? NumeroTarjeta { get; set; }
        public string? Instrucciones { get; set; }
        public bool Principal { get; set; }
        public bool Activo { get; set; }
    }
    public class GuardarCuentaBancariaAdLocalDto
    {
        public string Banco { get; set; } = string.Empty;
        public string Beneficiario { get; set; } = string.Empty;
        public string? NumeroCuenta { get; set; }
        public string? Clabe { get; set; }
        public string? NumeroTarjeta { get; set; }
        public string? Instrucciones { get; set; }
        public bool Principal { get; set; }
    }
}
