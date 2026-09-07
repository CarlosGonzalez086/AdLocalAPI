using System;
using AdLocalAPI.DTOs.Carrito;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class AgregarProductoCarritoDtoValidator : AbstractValidator<AgregarProductoCarritoDto>
    {
        public AgregarProductoCarritoDtoValidator()
        {
            RuleFor(x => x.ProductoUuid)
                .NotEmpty().WithMessage("El identificador del producto es obligatorio")
                .Must(id => id != Guid.Empty).WithMessage("El identificador del producto no puede ser vacío");

            RuleFor(x => x.Cantidad)
                .InclusiveBetween(1, 100).WithMessage("La cantidad debe estar entre 1 y 100 unidades");

            RuleFor(x => x.Observaciones)
                .MaximumLength(300).WithMessage("Las observaciones no pueden exceder 300 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Observaciones));
        }
    }

    public class ActualizarCantidadCarritoDtoValidator : AbstractValidator<ActualizarCantidadCarritoDto>
    {
        public ActualizarCantidadCarritoDtoValidator()
        {
            RuleFor(x => x.DetalleUuid)
                .NotEmpty().WithMessage("El identificador del item del carrito es obligatorio")
                .Must(id => id != Guid.Empty).WithMessage("El identificador del item del carrito no puede ser vacío");

            RuleFor(x => x.Cantidad)
                .InclusiveBetween(1, 100).WithMessage("La cantidad debe estar entre 1 y 100 unidades");
        }
    }
}
