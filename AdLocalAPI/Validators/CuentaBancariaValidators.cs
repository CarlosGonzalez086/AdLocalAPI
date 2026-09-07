using System.Text.RegularExpressions;
using AdLocalAPI.DTOs;
using static AdLocalAPI.DTOs.PagosComercio;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class GuardarCuentaBancariaAdLocalDtoValidator : AbstractValidator<GuardarCuentaBancariaAdLocalDto>
    {
        public GuardarCuentaBancariaAdLocalDtoValidator()
        {
            RuleFor(x => x.Banco)
                .NotEmpty().WithMessage("El nombre del banco es obligatorio")
                .MinimumLength(2).WithMessage("El nombre del banco debe tener al menos 2 caracteres")
                .MaximumLength(100).WithMessage("El nombre del banco no puede exceder 100 caracteres");

            RuleFor(x => x.Beneficiario)
                .NotEmpty().WithMessage("El nombre del beneficiario es obligatorio")
                .MinimumLength(2).WithMessage("El nombre del beneficiario debe tener al menos 2 caracteres")
                .MaximumLength(150).WithMessage("El nombre del beneficiario no puede exceder 150 caracteres");

            RuleFor(x => x.Clabe)
                .Matches(@"^\d{18}$").WithMessage("La CLABE interbancaria debe contener exactamente 18 dígitos numéricos")
                .When(x => !string.IsNullOrEmpty(x.Clabe));

            RuleFor(x => x.NumeroCuenta)
                .Matches(@"^\d{10,16}$").WithMessage("El número de cuenta debe contener entre 10 y 16 dígitos numéricos")
                .When(x => !string.IsNullOrEmpty(x.NumeroCuenta));

            RuleFor(x => x.NumeroTarjeta)
                .Matches(@"^\d{16}$").WithMessage("El número de tarjeta debe contener exactamente 16 dígitos numéricos")
                .When(x => !string.IsNullOrEmpty(x.NumeroTarjeta));

            RuleFor(x => x)
                .Must(x => !string.IsNullOrEmpty(x.Clabe) || !string.IsNullOrEmpty(x.NumeroCuenta) || !string.IsNullOrEmpty(x.NumeroTarjeta))
                .WithMessage("Debe proporcionar al menos un medio de depósito (CLABE, número de cuenta o número de tarjeta)");

            RuleFor(x => x.Instrucciones)
                .MaximumLength(500).WithMessage("Las instrucciones no pueden exceder 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Instrucciones));
        }
    }

    public class CuentaBancariaComercioCreateDtoValidator : AbstractValidator<CuentaBancariaComercioCreateDto>
    {
        public CuentaBancariaComercioCreateDtoValidator()
        {
            RuleFor(x => x.Banco)
                .NotEmpty().WithMessage("El nombre del banco es obligatorio")
                .MinimumLength(2).WithMessage("El nombre del banco debe tener al menos 2 caracteres")
                .MaximumLength(100).WithMessage("El nombre del banco no puede exceder 100 caracteres");

            RuleFor(x => x.Beneficiario)
                .NotEmpty().WithMessage("El nombre del beneficiario es obligatorio")
                .MinimumLength(2).WithMessage("El nombre del beneficiario debe tener al menos 2 caracteres")
                .MaximumLength(150).WithMessage("El nombre del beneficiario no puede exceder 150 caracteres");

            RuleFor(x => x.Clabe)
                .Matches(@"^\d{18}$").WithMessage("La CLABE interbancaria debe contener exactamente 18 dígitos numéricos")
                .When(x => !string.IsNullOrEmpty(x.Clabe));

            RuleFor(x => x.NumeroCuenta)
                .Matches(@"^\d{10,16}$").WithMessage("El número de cuenta debe contener entre 10 y 16 dígitos numéricos")
                .When(x => !string.IsNullOrEmpty(x.NumeroCuenta));

            RuleFor(x => x.NumeroTarjeta)
                .Matches(@"^\d{16}$").WithMessage("El número de tarjeta debe contener exactamente 16 dígitos numéricos")
                .When(x => !string.IsNullOrEmpty(x.NumeroTarjeta));

            RuleFor(x => x)
                .Must(x => !string.IsNullOrEmpty(x.Clabe) || !string.IsNullOrEmpty(x.NumeroCuenta) || !string.IsNullOrEmpty(x.NumeroTarjeta))
                .WithMessage("Debe proporcionar al menos un medio de depósito (CLABE, número de cuenta o número de tarjeta)");
        }
    }

    public class CuentaBancariaComercioUpdateDtoValidator : AbstractValidator<CuentaBancariaComercioUpdateDto>
    {
        public CuentaBancariaComercioUpdateDtoValidator()
        {
            RuleFor(x => x.Banco)
                .NotEmpty().WithMessage("El nombre del banco es obligatorio")
                .MinimumLength(2).WithMessage("El nombre del banco debe tener al menos 2 caracteres")
                .MaximumLength(100).WithMessage("El nombre del banco no puede exceder 100 caracteres");

            RuleFor(x => x.Beneficiario)
                .NotEmpty().WithMessage("El nombre del beneficiario es obligatorio")
                .MinimumLength(2).WithMessage("El nombre del beneficiario debe tener al menos 2 caracteres")
                .MaximumLength(150).WithMessage("El nombre del beneficiario no puede exceder 150 caracteres");

            RuleFor(x => x.Clabe)
                .Matches(@"^\d{18}$").WithMessage("La CLABE interbancaria debe contener exactamente 18 dígitos numéricos")
                .When(x => !string.IsNullOrEmpty(x.Clabe));

            RuleFor(x => x.NumeroCuenta)
                .Matches(@"^\d{10,16}$").WithMessage("El número de cuenta debe contener entre 10 y 16 dígitos numéricos")
                .When(x => !string.IsNullOrEmpty(x.NumeroCuenta));

            RuleFor(x => x.NumeroTarjeta)
                .Matches(@"^\d{16}$").WithMessage("El número de tarjeta debe contener exactamente 16 dígitos numéricos")
                .When(x => !string.IsNullOrEmpty(x.NumeroTarjeta));

            RuleFor(x => x)
                .Must(x => !string.IsNullOrEmpty(x.Clabe) || !string.IsNullOrEmpty(x.NumeroCuenta) || !string.IsNullOrEmpty(x.NumeroTarjeta))
                .WithMessage("Debe proporcionar al menos un medio de depósito (CLABE, número de cuenta o número de tarjeta)");
        }
    }

    public class ConfiguracionPagoComercioDtoValidator : AbstractValidator<ConfiguracionPagoComercioDto>
    {
        public ConfiguracionPagoComercioDtoValidator()
        {
            RuleFor(x => x)
                .Must(x => x.AceptaEfectivo || x.AceptaTransferencia)
                .WithMessage("El comercio debe aceptar al menos un método de pago (efectivo o transferencia)");

            RuleFor(x => x.CostoEnvio)
                .GreaterThanOrEqualTo(0m).WithMessage("El costo de envío no puede ser negativo");

            RuleFor(x => x.CompraMinimaEnvioGratis)
                .GreaterThanOrEqualTo(0m).WithMessage("La compra mínima para envío gratis no puede ser negativa")
                .When(x => x.CompraMinimaEnvioGratis.HasValue);

            RuleFor(x => x.InstruccionesTransferencia)
                .MaximumLength(500).WithMessage("Las instrucciones de transferencia no pueden exceder 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.InstruccionesTransferencia));
        }
    }
}
