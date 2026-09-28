using System.Text.RegularExpressions;
using AdLocalAPI.DTOs.Direcciones;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class DireccionUsuarioDtoValidator : AbstractValidator<DireccionUsuarioDto>
    {
        public DireccionUsuarioDtoValidator()
        {
            RuleFor(x => x.Alias)
                .NotEmpty().WithMessage("El alias de la dirección es obligatorio")
                .MinimumLength(2).WithMessage("El alias debe tener al menos 2 caracteres")
                .MaximumLength(50).WithMessage("El alias no puede exceder 50 caracteres");

            RuleFor(x => x.Calle)
                .NotEmpty().WithMessage("La calle es obligatoria")
                .MinimumLength(2).WithMessage("La calle debe tener al menos 2 caracteres")
                .MaximumLength(200).WithMessage("La calle no puede exceder 200 caracteres");

            RuleFor(x => x.NumeroExterior)
                .NotEmpty().WithMessage("El número exterior es obligatorio")
                .MaximumLength(20).WithMessage("El número exterior no puede exceder 20 caracteres");

            RuleFor(x => x.NumeroInterior)
                .MaximumLength(20).WithMessage("El número interior no puede exceder 20 caracteres")
                .When(x => !string.IsNullOrEmpty(x.NumeroInterior));

            RuleFor(x => x.Colonia)
                .NotEmpty().WithMessage("La colonia es obligatoria")
                .MinimumLength(2).WithMessage("La colonia debe tener al menos 2 caracteres")
                .MaximumLength(150).WithMessage("La colonia no puede exceder 150 caracteres");

            RuleFor(x => x.CodigoPostal)
                .NotEmpty().WithMessage("El código postal es obligatorio")
                .Matches(@"^\d{5}$").WithMessage("El código postal debe tener exactamente 5 dígitos numéricos");

            RuleFor(x => x.IdEstado)
                .GreaterThan(0).WithMessage("Debe seleccionar un estado válido");

            RuleFor(x => x.IdMunicipio)
                .GreaterThan(0).WithMessage("Debe seleccionar un municipio válido");

            RuleFor(x => x.Latitud)
                .InclusiveBetween(-90m, 90m).WithMessage("La latitud debe estar entre -90 y 90 grados")
                .When(x => x.Latitud.HasValue);

            RuleFor(x => x.Longitud)
                .InclusiveBetween(-180m, 180m).WithMessage("La longitud debe estar entre -180 y 180 grados")
                .When(x => x.Longitud.HasValue);

            RuleFor(x => x.Referencias)
                .MaximumLength(500).WithMessage("Las referencias no pueden exceder 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Referencias));

            RuleFor(x => x.Telefono)
                .Matches(@"^\d{10}$").WithMessage("El teléfono debe contener 10 dígitos numéricos")
                .When(x => !string.IsNullOrEmpty(x.Telefono));
        }
    }
}
