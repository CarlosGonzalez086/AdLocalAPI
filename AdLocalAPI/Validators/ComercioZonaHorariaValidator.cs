using AdLocalAPI.DTOs;
using AdLocalAPI.Helpers;
using FluentValidation;

namespace AdLocalAPI.Validators;

public class ComercioZonaHorariaValidator : AbstractValidator<ComercioZonaHorariaDto>
{
    public ComercioZonaHorariaValidator()
    {
        RuleFor(x => x.ZonaHoraria).Must(CitaOperacion.ZonaValida).WithMessage("Selecciona una zona IANA válida.");
        RuleFor(x => x.ZonaHorariaAnterior).NotEmpty().MaximumLength(100);
    }
}
