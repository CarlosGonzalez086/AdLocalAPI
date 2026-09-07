using AdLocalAPI.DTOs;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class PlanCreateDtoValidator : AbstractValidator<PlanCreateDto>
    {
        public PlanCreateDtoValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del plan es obligatorio")
                .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
                .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

            RuleFor(x => x.StripePriceId)
                .NotEmpty().WithMessage("El identificador de precio de Stripe es obligatorio")
                .Must(id => id.StartsWith("price_") || id.StartsWith("prod_"))
                .WithMessage("El identificador de Stripe debe tener un formato válido (price_... o prod_...)");

            RuleFor(x => x.Precio)
                .GreaterThanOrEqualTo(0m).WithMessage("El precio del plan no puede ser negativo");

            RuleFor(x => x.DuracionDias)
                .GreaterThan(0).WithMessage("La duración en días debe ser mayor a 0");

            RuleFor(x => x.Tipo)
                .NotEmpty().WithMessage("El tipo de plan es obligatorio")
                .MaximumLength(50).WithMessage("El tipo de plan no puede exceder 50 caracteres");

            RuleFor(x => x.MaxNegocios)
                .GreaterThanOrEqualTo(0).WithMessage("El límite de negocios no puede ser negativo");

            RuleFor(x => x.MaxProductos)
                .GreaterThanOrEqualTo(0).WithMessage("El límite de productos no puede ser negativo");

            RuleFor(x => x.MaxFotos)
                .GreaterThanOrEqualTo(0).WithMessage("El límite de fotos no puede ser negativo");

            RuleFor(x => x.NivelVisibilidad)
                .GreaterThanOrEqualTo(0).WithMessage("El nivel de visibilidad no puede ser negativo");

            RuleFor(x => x.BadgeTexto)
                .MaximumLength(50).WithMessage("El texto del badge no puede exceder 50 caracteres")
                .When(x => !string.IsNullOrEmpty(x.BadgeTexto));
        }
    }
}
