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
            RuleFor(x => x.VersionPedido).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Comentario).NotEmpty().When(x => x.Estado == EstadoPedido.Rechazado || x.Estado == EstadoPedido.Cancelado);
            RuleForEach(x => x.ProductosRestituibles!).NotNull().ChildRules(d =>
            {
                d.RuleFor(x => x.DetalleUuid).NotEmpty();
                d.RuleFor(x => x.Cantidad).GreaterThan(0);
            }).When(x => x.ProductosRestituibles != null);

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
            RuleFor(x => x.VersionPedido).GreaterThanOrEqualTo(0);

            RuleFor(x => x.Comentario)
                .MaximumLength(500).WithMessage("El comentario no puede exceder 500 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Comentario));
        }
    }

    public class RegistrarReembolsoPedidoDtoValidator : AbstractValidator<RegistrarReembolsoPedidoDto>
    {
        public RegistrarReembolsoPedidoDtoValidator()
        {
            RuleFor(x => x.VersionPedido).GreaterThanOrEqualTo(0);
            RuleFor(x => x.IdempotencyKey).NotEmpty();
            RuleFor(x => x.Monto).GreaterThan(0);
            RuleFor(x => x.Metodo).IsInEnum();
            RuleFor(x => x.Referencia).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Motivo).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ProductosDevueltos).NotNull();
            RuleForEach(x => x.ProductosDevueltos).NotNull().ChildRules(d =>
            {
                d.RuleFor(x => x.DetalleUuid).NotEmpty();
                d.RuleFor(x => x.Cantidad).GreaterThan(0);
            });
        }
    }
    public class ConciliarInventarioPedidoDtoValidator : AbstractValidator<ConciliarInventarioPedidoDto>
    {
        public ConciliarInventarioPedidoDtoValidator()
        {
            RuleFor(x => x.VersionPedido).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Referencia).NotEmpty().MaximumLength(500);
            RuleFor(x => x.StockDescontado).NotNull();
            RuleForEach(x => x.StockDescontado).NotNull().ChildRules(d =>
            {
                d.RuleFor(x => x.DetalleUuid).NotEmpty();
                d.RuleFor(x => x.Cantidad).GreaterThanOrEqualTo(0);
            });
        }
    }
}
