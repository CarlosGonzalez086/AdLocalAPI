namespace AdLocalAPI.Utils
{
    public enum EstadoPedido
    {
        PendienteAprobacion = 1,
        Aprobado = 2,
        Rechazado = 3,
        Preparando = 4,
        ListoParaRecoger = 5,
        ListoParaEnviar = 6,
        Enviado = 7,
        Entregado = 8,
        Completado = 9,
        Cancelado = 10
    }

    public enum EstadoPagoPedido
    {
        Pendiente = 1,
        PendienteComprobante = Pendiente,
        PendienteVerificacion = 2,
        Pagado = 3,
        Rechazado = 4,
        Reembolsado = 5
    }

    public enum MetodoPagoPedido
    {
        Efectivo = 1,
        Transferencia = 2
    }

    public enum TipoEntregaPedido
    {
        Recoger = 1,
        Domicilio = 2
    }
}
