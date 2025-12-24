namespace AdLocalAPI.Models
{
    public class ApiResponse<T>
    {
        public string Codigo { get; set; }
        public string Mensaje { get; set; }
        public T Respuesta { get; set; }

        public static ApiResponse<T> Success(T data, string mensaje = "Operación exitosa")
        {
            return new ApiResponse<T>
            {
                Codigo = "200",
                Mensaje = mensaje,
                Respuesta = data
            };
        }

        public static ApiResponse<T> Error(string codigo, string mensaje)
        {
            return new ApiResponse<T>
            {
                Codigo = codigo,
                Mensaje = mensaje,
                Respuesta = default
            };
        }
    }

}
