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

    }
}
