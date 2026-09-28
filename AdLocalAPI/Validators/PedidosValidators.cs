using AdLocalAPI.DTOs;
using AdLocalAPI.Utils;
using FluentValidation;

namespace AdLocalAPI.Validators
{
    public class CambiarEstadoPedidoDtoValidator : AbstractValidator<CambiarEstadoPedidoDto>
    {
        public CambiarEstadoPedidoDtoValidator()
        {
            RuleFor(x => x.Estado)
                .IsInEnum().WithMessage("El estado de pedido especificado no es válido");

            RuleFor(x => x.Comentario)
                .MaximumLength(500).WithMessage("El comentario no puede exceder 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Comentario));
        }
    }

    public class RevisarPagoPedidoDtoValidator : AbstractValidator<RevisarPagoPedidoDto>
    {
        public RevisarPagoPedidoDtoValidator()
        {
            RuleFor(x => x.EstadoPago)
                .IsInEnum().WithMessage("El estado de pago especificado no es válido");

            RuleFor(x => x.Comentario)
                .MaximumLength(500).WithMessage("El comentario no puede exceder 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Comentario));
        }
    }
}
