namespace AdLocalAPI.Utils
{
    public class Enums
    {
        public static class PlanTipos
        {
            public const string Basico = "Basico";
            public const string Premium = "Premium";
            public const string Empresarial = "Empresarial";

            public static readonly HashSet<string> Validos = new()
            {
                Basico,
                Premium,
                Empresarial
            };
        }
        public enum TipoProductoServicio
        {
            Producto = 1,
            Servicio = 2
        }

        public enum ModalidadProductoServicio
        {
            Compra = 1,
            Reservacion = 2,
            Cotizacion = 3
        }

    }
}
