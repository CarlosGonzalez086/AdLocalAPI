using System;
using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Utils;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class CheckoutComercioDtoValidator : AbstractValidator<CheckoutComercioDto>
    {
        public CheckoutComercioDtoValidator()
        {
            RuleFor(x => x.ComercioUuid)
                .NotEmpty().WithMessage("El UUID del comercio es obligatorio")
                .Must(id => id != Guid.Empty).WithMessage("El UUID del comercio no puede ser vacío");

            RuleFor(x => x.TipoEntrega)
                .IsInEnum().WithMessage("El tipo de entrega seleccionado no es válido");

            RuleFor(x => x.MetodoPago)
                .IsInEnum().WithMessage("El método de pago seleccionado no es válido");

            RuleFor(x => x.DireccionUuid)
                .NotNull().WithMessage("La dirección de entrega es obligatoria para pedidos con entrega a domicilio")
                .Must(d => d.HasValue && d.Value != Guid.Empty).WithMessage("El identificador de la dirección no puede ser vacío")
                .When(x => x.TipoEntrega == TipoEntregaPedido.Domicilio);

            RuleFor(x => x.Observaciones)
                .MaximumLength(500).WithMessage("Las observaciones no pueden exceder 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Observaciones));
        }
    }

    public class ConfirmarCheckoutDtoValidator : AbstractValidator<ConfirmarCheckoutDto>
    {
        public ConfirmarCheckoutDtoValidator()
        {
            RuleFor(x => x.Comercios)
                .NotEmpty().WithMessage("Debe incluir al menos un comercio para confirmar el checkout")
                .Must(c => c != null && c.Count > 0).WithMessage("La lista de comercios no puede estar vacía");

            RuleForEach(x => x.Comercios)
                .SetValidator(new CheckoutComercioDtoValidator());

            RuleFor(x => x.IdempotencyKey)
                .MaximumLength(100).WithMessage("La clave de idempotencia no puede exceder 100 caracteres")
                .When(x => !string.IsNullOrEmpty(x.IdempotencyKey));
        }
    }

    public class CheckoutRequestDtoValidator : AbstractValidator<CheckoutRequestDto>
    {
        public CheckoutRequestDtoValidator()
        {
            RuleFor(x => x.PlanId)
                .GreaterThan(0).WithMessage("El identificador del plan debe ser mayor a 0");

            RuleFor(x => x.Metodo)
                .NotEmpty().WithMessage("El método de pago es obligatorio")
                .MaximumLength(50).WithMessage("El método no puede exceder 50 caracteres");

            RuleFor(x => x.StripePaymentMethodId)
                .NotEmpty().WithMessage("El método de pago de Stripe es obligatorio para pagos con tarjeta")
                .When(x => string.Equals(x.Metodo, "STRIPE", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(x.Metodo, "TARJETA", StringComparison.OrdinalIgnoreCase));
        }
    }

    public class CambiarPlanDtoValidator : AbstractValidator<CambiarPlanDto>
    {
        public CambiarPlanDtoValidator()
        {
            RuleFor(x => x.PlanIdNuevo)
                .GreaterThan(0).WithMessage("El nuevo plan debe ser mayor a 0");

            RuleFor(x => x.Metodo)
                .NotEmpty().WithMessage("El método de pago es obligatorio")
                .MaximumLength(50).WithMessage("El método no puede exceder 50 caracteres");
        }
    }

    public class CreateCheckoutDtoValidator : AbstractValidator<CreateCheckoutDto>
    {
        public CreateCheckoutDtoValidator()
        {
            RuleFor(x => x.PlanId)
                .GreaterThan(0).WithMessage("El identificador del plan debe ser mayor a 0");

            RuleFor(x => x.PlanTipo)
                .NotEmpty().WithMessage("El tipo de plan es obligatorio")
                .MaximumLength(50).WithMessage("El tipo de plan no puede exceder 50 caracteres");
        }
    }
}
