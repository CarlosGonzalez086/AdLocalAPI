using AdLocalAPI.DTOs;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class ProductosServiciosDtoValidator : AbstractValidator<ProductosServiciosDto>
    {
        public ProductosServiciosDtoValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es obligatorio")
                .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
                .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres");

            RuleFor(x => x.Descripcion)
                .MaximumLength(500).WithMessage("La descripción no puede exceder 400 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Descripcion)); 

            RuleFor(x => x.Tipo)
                .InclusiveBetween(1, 2)
                .WithMessage("El tipo debe estar entre 1 y 2 (Producto/Servicio válido)");



            RuleFor(x => x.Precio)
                .GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo")
                .LessThanOrEqualTo(999999999).WithMessage("Precio fuera de rango permitido")
                .When(x => x.Precio.HasValue);

            RuleFor(x => x.Stock)
                .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo")
                .LessThanOrEqualTo(999999).WithMessage("Stock fuera de rango")
                .When(x => x.Stock.HasValue);


            RuleFor(x => x)
                .Must(x => x.Precio.HasValue || x.Tipo != 1) 
                .WithMessage("Los productos deben tener precio definido");


            RuleFor(x => x.Stock)
                .NotNull().WithMessage("El stock es obligatorio para productos físicos")
                .When(x => x.Tipo == 1); 
        }
    }
}
