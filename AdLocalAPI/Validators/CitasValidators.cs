using System;
using AdLocalAPI.DTOs;
using AdLocalAPI.Models;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class CrearCitaDtoValidator : AbstractValidator<CrearCitaDto>
    {
        public CrearCitaDtoValidator()
        {
            RuleFor(x => x.ZonaHoraria).Must(Helpers.CitaOperacion.ZonaValida).WithMessage("Zona horaria no válida");
            RuleFor(x => x.IdempotencyKey).NotEmpty();
            RuleFor(x => x.ProductoUuid)
                .NotEmpty().WithMessage("El identificador del servicio es obligatorio")
                .Must(id => id != Guid.Empty).WithMessage("El identificador del servicio no puede ser vacío");

            RuleFor(x => x.FechaInicio)
                .NotEmpty().WithMessage("La fecha y hora de la cita es obligatoria")
                .Must(x => x.Kind == DateTimeKind.Unspecified && x.Ticks % TimeSpan.TicksPerMinute == 0)
                .WithMessage("Utiliza la hora local del comercio sin zona ni segundos adicionales");

            RuleFor(x => x.NombrePersona)
                .NotEmpty().WithMessage("El nombre de la persona que asistirá es obligatorio")
                .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
                .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

            RuleFor(x => x.Notas)
                .MaximumLength(500).WithMessage("Las notas no pueden exceder 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Notas));
        }
    }

    public class ActualizarCitaComercioDtoValidator : AbstractValidator<ActualizarCitaComercioDto>
    {
        public ActualizarCitaComercioDtoValidator()
        {
            RuleFor(x => x.VersionCita).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Estado)
                .IsInEnum().WithMessage("El estado de la cita no es válido");

            RuleFor(x => x.NombreAtiende)
                .MaximumLength(100).WithMessage("El nombre de quien atiende no puede exceder 100 caracteres")
                .When(x => !string.IsNullOrEmpty(x.NombreAtiende));

            RuleFor(x => x.Motivo)
                .MaximumLength(500).WithMessage("El motivo no puede exceder 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Motivo));
        }
    }

    public class ReprogramarCitaDtoValidator : AbstractValidator<ReprogramarCitaDto>
    {
        public ReprogramarCitaDtoValidator()
        {
            RuleFor(x => x.ZonaHoraria).Must(Helpers.CitaOperacion.ZonaValida).WithMessage("Zona horaria no válida");
            RuleFor(x => x.VersionCita).GreaterThanOrEqualTo(0);
            RuleFor(x => x.FechaInicio)
                .NotEmpty().WithMessage("La nueva fecha y hora de la cita es obligatoria")
                .Must(x => x.Kind == DateTimeKind.Unspecified && x.Ticks % TimeSpan.TicksPerMinute == 0)
                .WithMessage("Utiliza la hora local del comercio sin zona ni segundos adicionales");
        }
    }

    public class CancelarCitaDtoValidator : AbstractValidator<CancelarCitaDto>
    {
        public CancelarCitaDtoValidator()
        {
            RuleFor(x => x.VersionCita).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
        }
    }
}
