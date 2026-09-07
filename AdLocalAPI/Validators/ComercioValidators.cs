using System;
using AdLocalAPI.DTOs;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class ComercioCreateDtoValidator : AbstractValidator<ComercioCreateDto>
    {
        public ComercioCreateDtoValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del comercio es obligatorio")
                .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
                .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres");

            RuleFor(x => x.Direccion)
                .NotEmpty().WithMessage("La dirección del comercio es obligatoria")
                .MinimumLength(5).WithMessage("La dirección debe tener al menos 5 caracteres")
                .MaximumLength(300).WithMessage("La dirección no puede exceder 300 caracteres");

            RuleFor(x => x.Telefono)
                .Matches(@"^\d{10}$").WithMessage("El teléfono debe contener exactamente 10 dígitos numéricos")
                .When(x => !string.IsNullOrEmpty(x.Telefono));

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("El correo electrónico del comercio no es válido")
                .MaximumLength(150).WithMessage("El correo electrónico no puede exceder 150 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Descripcion)
                .MaximumLength(1000).WithMessage("La descripción no puede exceder 1000 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Descripcion));

            RuleFor(x => x.Lat)
                .InclusiveBetween(-90.0, 90.0).WithMessage("La latitud debe estar entre -90 y 90 grados");

            RuleFor(x => x.Lng)
                .InclusiveBetween(-180.0, 180.0).WithMessage("La longitud debe estar entre -180 y 180 grados");

            RuleFor(x => x.EstadoId)
                .GreaterThan(0).WithMessage("Debe seleccionar un estado válido");

            RuleFor(x => x.MunicipioId)
                .GreaterThan(0).WithMessage("Debe seleccionar un municipio válido");

            RuleFor(x => x.TipoComercioId)
                .GreaterThan(0).WithMessage("Debe seleccionar un tipo de comercio válido");
        }
    }

    public class ComercioUpdateDtoValidator : AbstractValidator<ComercioUpdateDto>
    {
        public ComercioUpdateDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("El identificador del comercio debe ser mayor a 0");

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del comercio es obligatorio")
                .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
                .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres");

            RuleFor(x => x.Direccion)
                .NotEmpty().WithMessage("La dirección del comercio es obligatoria")
                .MinimumLength(5).WithMessage("La dirección debe tener al menos 5 caracteres")
                .MaximumLength(300).WithMessage("La dirección no puede exceder 300 caracteres");

            RuleFor(x => x.Telefono)
                .Matches(@"^\d{10}$").WithMessage("El teléfono debe contener exactamente 10 dígitos numéricos")
                .When(x => !string.IsNullOrEmpty(x.Telefono));

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("El correo electrónico del comercio no es válido")
                .MaximumLength(150).WithMessage("El correo electrónico no puede exceder 150 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Descripcion)
                .MaximumLength(1000).WithMessage("La descripción no puede exceder 1000 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Descripcion));

            RuleFor(x => x.Lat)
                .InclusiveBetween(-90.0, 90.0).WithMessage("La latitud debe estar entre -90 y 90 grados");

            RuleFor(x => x.Lng)
                .InclusiveBetween(-180.0, 180.0).WithMessage("La longitud debe estar entre -180 y 180 grados");

            RuleFor(x => x.EstadoId)
                .GreaterThan(0).WithMessage("Debe seleccionar un estado válido");

            RuleFor(x => x.MunicipioId)
                .GreaterThan(0).WithMessage("Debe seleccionar un municipio válido");

            RuleFor(x => x.TipoComercioId)
                .GreaterThan(0).WithMessage("Debe seleccionar un tipo de comercio válido");
        }
    }

    public class ColaborarDtoValidator : AbstractValidator<ColaborarDto>
    {
        public ColaborarDtoValidator()
        {
            RuleFor(x => x.IdComercio)
                .GreaterThan(0).WithMessage("El identificador del comercio debe ser mayor a 0");

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre del colaborador es obligatorio")
                .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
                .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

            RuleFor(x => x.Correo)
                .NotEmpty().WithMessage("El correo electrónico del colaborador es obligatorio")
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido")
                .MaximumLength(150).WithMessage("El correo electrónico no puede exceder 150 caracteres");
        }
    }
}
