using System;
using AdLocalAPI.DTOs;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class CrearCotizacionDtoValidator : AbstractValidator<CrearCotizacionDto>
    {
        public CrearCotizacionDtoValidator()
        {
            RuleFor(x => x.ProductoUuid)
                .NotEmpty().WithMessage("El identificador del servicio a cotizar es obligatorio")
                .Must(id => id != Guid.Empty).WithMessage("El identificador del servicio no puede ser vacío");

            RuleFor(x => x.Solicitud)
                .NotEmpty().WithMessage("El detalle de la solicitud de cotización es obligatorio")
                .MinimumLength(5).WithMessage("La solicitud debe tener al menos 5 caracteres descriptivos")
                .MaximumLength(1000).WithMessage("La solicitud no puede exceder 1000 caracteres");
        }
    }
}
