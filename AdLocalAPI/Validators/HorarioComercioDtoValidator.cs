using System;
using AdLocalAPI.DTOs;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class HorarioComercioDtoValidator : AbstractValidator<HorarioComercioDto>
    {
        public HorarioComercioDtoValidator()
        {
            RuleFor(x => x.Dia)
                .IsInEnum().WithMessage("El día de la semana no es válido");

            RuleFor(x => x.HoraApertura)
                .NotNull().WithMessage("La hora de apertura es obligatoria si el comercio abre este día")
                .When(x => x.Abierto);

            RuleFor(x => x.HoraCierre)
                .NotNull().WithMessage("La hora de cierre es obligatoria si el comercio abre este día")
                .GreaterThan(x => x.HoraApertura!.Value)
                .WithMessage("La hora de cierre debe ser posterior a la hora de apertura")
                .When(x => x.Abierto && x.HoraApertura.HasValue);
        }
    }
}
