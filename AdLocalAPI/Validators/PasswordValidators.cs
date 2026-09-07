using AdLocalAPI.DTOs;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(x => x.PasswordActual)
                .NotEmpty().WithMessage("La contraseña actual es obligatoria");

            RuleFor(x => x.PasswordNueva)
                .NotEmpty().WithMessage("La nueva contraseña es obligatoria")
                .MinimumLength(8).WithMessage("La nueva contraseña debe tener al menos 8 caracteres")
                .MaximumLength(100).WithMessage("La nueva contraseña no puede exceder 100 caracteres")
                .NotEqual(x => x.PasswordActual).WithMessage("La nueva contraseña no puede ser igual a la contraseña actual");
        }
    }

    public class NewPasswordDtoValidator : AbstractValidator<NewPasswordDto>
    {
        public NewPasswordDtoValidator()
        {
            RuleFor(x => x.PasswordNueva)
                .NotEmpty().WithMessage("La nueva contraseña es obligatoria")
                .MinimumLength(8).WithMessage("La nueva contraseña debe tener al menos 8 caracteres")
                .MaximumLength(100).WithMessage("La nueva contraseña no puede exceder 100 caracteres");

            RuleFor(x => x.Codigo)
                .NotEmpty().WithMessage("El código de verificación es obligatorio")
                .Length(4, 10).WithMessage("El código de verificación debe tener entre 4 y 10 caracteres");
        }
    }

    public class RestablecerPasswordDtoValidator : AbstractValidator<RestablecerPasswordDto>
    {
        public RestablecerPasswordDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio")
                .MaximumLength(150).WithMessage("El correo no puede exceder 150 caracteres")
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido");

            RuleFor(x => x.Codigo)
                .NotEmpty().WithMessage("El código de verificación es obligatorio")
                .Length(4, 10).WithMessage("El código de verificación debe tener entre 4 y 10 caracteres");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria")
                .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
                .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres");

            RuleFor(x => x.ConfirmarPassword)
                .NotEmpty().WithMessage("Debe confirmar la contraseña")
                .Equal(x => x.Password).WithMessage("Las contraseñas no coinciden");
        }
    }

    public class VerificarCodigoDtoValidator : AbstractValidator<VerificarCodigoDto>
    {
        public VerificarCodigoDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio")
                .MaximumLength(150).WithMessage("El correo no puede exceder 150 caracteres")
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido");

            RuleFor(x => x.Codigo)
                .NotEmpty().WithMessage("El código de verificación es obligatorio")
                .Length(4, 10).WithMessage("El código de verificación debe tener entre 4 y 10 caracteres");
        }
    }

    public class EmailDtoValidator : AbstractValidator<EmailDto>
    {
        public EmailDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio")
                .MaximumLength(150).WithMessage("El correo no puede exceder 150 caracteres")
                .EmailAddress().WithMessage("El formato del correo electrónico no es válido");
        }
    }
}
