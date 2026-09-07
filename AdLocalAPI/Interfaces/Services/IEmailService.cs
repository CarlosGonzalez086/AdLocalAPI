using System.Threading.Tasks;

namespace AdLocalAPI.Interfaces.Services
{
    public interface IEmailService
    {
        Task EnviarCorreoAsync(string para, string asunto, string htmlContenido);
    }
}
