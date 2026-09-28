using AdLocalAPI.DTOs;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class CalificacionComentarioCreateDtoValidator : AbstractValidator<CalificacionComentarioCreateDto>
    {
        public CalificacionComentarioCreateDtoValidator()
        {
            RuleFor(x => x.Calificacion)
                .InclusiveBetween(1, 5).WithMessage("La calificación debe estar entre 1 y 5 estrellas");

            RuleFor(x => x.IdComercio)
                .GreaterThan(0).WithMessage("El identificador del comercio debe ser mayor a 0");

            RuleFor(x => x.NombrePersona)
                .NotEmpty().WithMessage("El nombre de la persona es obligatorio")
                .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
                .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

            RuleFor(x => x.Comentario)
                .MaximumLength(1000).WithMessage("El comentario no puede exceder 1000 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Comentario));
        }
    }
}
