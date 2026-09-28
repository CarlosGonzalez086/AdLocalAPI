using AdLocalAPI.DTOs;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class CrearSuscripcionDtoValidator : AbstractValidator<CrearSuscripcionDto>
    {
        public CrearSuscripcionDtoValidator()
        {
            RuleFor(x => x.PlanId)
                .GreaterThan(0).WithMessage("El identificador del plan debe ser mayor a 0");

            RuleFor(x => x.StripePaymentMethodId)
                .NotEmpty().WithMessage("El método de pago de Stripe es obligatorio")
                .Must(id => id.StartsWith("pm_"))
                .WithMessage("El método de pago de Stripe debe comenzar con 'pm_'");
        }
    }

    public class CrearTarjetaDtoValidator : AbstractValidator<CrearTarjetaDto>
    {
        public CrearTarjetaDtoValidator()
        {
            RuleFor(x => x.PaymentMethodId)
                .NotEmpty().WithMessage("El identificador del método de pago es obligatorio")
                .Must(id => id.StartsWith("pm_"))
                .WithMessage("El identificador del método de pago de Stripe debe comenzar con 'pm_'");
        }
    }
}
