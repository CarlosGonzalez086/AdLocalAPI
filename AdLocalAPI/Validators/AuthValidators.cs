using System.Text.RegularExpressions;
using AdLocalAPI.DTOs;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class UsuarioRegistroDtoValidator : AbstractValidator<UsuarioRegistroDto>
    {
        public UsuarioRegistroDtoValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es obligatorio")
                .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
                .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio")
                .MaximumLength(150).WithMessage("El correo no puede exceder 150 caracteres")
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria")
                .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
                .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres");

            RuleFor(x => x.CodigoReferenciado)
                .MaximumLength(50).WithMessage("El código referenciado no puede exceder 50 caracteres")
                .When(x => !string.IsNullOrEmpty(x.CodigoReferenciado));

            RuleFor(x => x.ComercioId)
                .GreaterThan(0).WithMessage("El ID de comercio debe ser mayor a 0")
                .When(x => x.ComercioId.HasValue);
        }
    }

    public class ClienteRegistroDtoValidator : AbstractValidator<ClienteRegistroDto>
    {
        public ClienteRegistroDtoValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es obligatorio")
                .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres")
                .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio")
                .MaximumLength(150).WithMessage("El correo no puede exceder 150 caracteres")
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria")
                .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
                .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres");

            RuleFor(x => x.ConfirmarPassword)
                .NotEmpty().WithMessage("Debe confirmar la contraseña")
                .Equal(x => x.Password).WithMessage("Las contraseñas no coinciden");
        }
    }

    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio")
                .MaximumLength(150).WithMessage("El correo no puede exceder 150 caracteres")
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria")
                .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres");
        }
    }
}
