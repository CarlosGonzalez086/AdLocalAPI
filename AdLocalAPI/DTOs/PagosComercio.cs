namespace AdLocalAPI.DTOs
{
    public class PagosComercio
    {
        public class ConfiguracionPagoComercioDto
        {
            public bool AceptaEfectivo { get; set; } = true;

            public bool AceptaTransferencia { get; set; } = false;

            public string? InstruccionesTransferencia { get; set; }
            public decimal CostoEnvio { get; set; }
            public decimal? CompraMinimaEnvioGratis { get; set; }

            public bool Activo { get; set; } = true;
        }

        public class ConfiguracionPagoComercioResponseDto
        {
            public Guid Uuid { get; set; }

            public long IdComercio { get; set; }

            public bool AceptaEfectivo { get; set; }

            public bool AceptaTransferencia { get; set; }

            public string? InstruccionesTransferencia { get; set; }
            public decimal CostoEnvio { get; set; }
            public decimal? CompraMinimaEnvioGratis { get; set; }

            public bool Activo { get; set; }

            public DateTime FechaCreacion { get; set; }

            public DateTime? FechaActualizacion { get; set; }
        }
        public class CuentaBancariaComercioCreateDto
        {
            public string Banco { get; set; } = string.Empty;

            public string Beneficiario { get; set; } = string.Empty;

            public string? NumeroCuenta { get; set; }

            public string? Clabe { get; set; }

            public string? NumeroTarjeta { get; set; }

            public bool Principal { get; set; } = false;
        }

        public class CuentaBancariaComercioUpdateDto
        {
            public string Banco { get; set; } = string.Empty;

            public string Beneficiario { get; set; } = string.Empty;

            public string? NumeroCuenta { get; set; }

            public string? Clabe { get; set; }

            public string? NumeroTarjeta { get; set; }

            public bool Principal { get; set; }

            public bool Activo { get; set; } = true;
        }

        public class CuentaBancariaComercioResponseDto
        {
            public Guid Uuid { get; set; }

            public long IdComercio { get; set; }

            public string Banco { get; set; } = string.Empty;

            public string Beneficiario { get; set; } = string.Empty;

            public string? NumeroCuenta { get; set; }

            public string? Clabe { get; set; }

            public string? NumeroTarjeta { get; set; }

            public bool Principal { get; set; }

            public bool Activo { get; set; }

            public DateTime FechaCreacion { get; set; }

            public DateTime? FechaActualizacion { get; set; }
        }
    }
}
