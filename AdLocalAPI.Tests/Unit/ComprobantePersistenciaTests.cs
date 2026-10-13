using System.Security.Claims;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories.Interfaces;
using AdLocalAPI.Services;
using AdLocalAPI.Services.Interfaces;
using AdLocalAPI.Utils;
using Amazon.S3;
using Amazon.S3.Model;
using PutObjectRequest = Amazon.S3.Model.PutObjectRequest;
using DeleteObjectRequest = Amazon.S3.Model.DeleteObjectRequest;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AdLocalAPI.Tests.Unit;

public class ComprobantePersistenciaTests
{
    [Theory]
    [InlineData(false, false, false, "200", 0)] // Notifications fail after a confirmed commit.
    [InlineData(true, true, false, "200", 0)] // Commit succeeded but its response was lost.
    [InlineData(true, false, true, "500", 0)] // Cannot prove absence: retain the object for reconciliation.
    [InlineData(true, false, false, "500", 1)] // Confirmed DB failure: clean the orphaned object.
    public async Task FallosDePersistenciaONotificacion_NoBorranComprobanteConfirmado(bool fallaDb, bool registrado,
        bool fallaVerificacion, string codigo, int borrados)
    {
        var uuid = Guid.NewGuid();
        var repo = new Mock<IPedidoRepository>();
        repo.Setup(x => x.ObtenerPedidoClienteAsync(uuid, 5)).ReturnsAsync(new Pedido { Id = 1, Uuid = uuid, IdUsuario = 5,
            MetodoPago = MetodoPagoPedido.Transferencia, Total = 90, EstadoPago = EstadoPagoPedido.Pendiente });
        if (fallaDb) repo.Setup(x => x.GuardarComprobanteAsync(It.IsAny<Pedido>(), It.IsAny<ComprobantePago>())).ThrowsAsync(new IOException("Fallo sintético de confirmación"));
        if (fallaVerificacion) repo.Setup(x => x.ComprobanteRegistradoAsync(It.IsAny<Guid>(), 5)).ThrowsAsync(new IOException("Base no disponible"));
        else repo.Setup(x => x.ComprobanteRegistradoAsync(It.IsAny<Guid>(), 5)).ReturnsAsync(registrado);
        var s3 = new Mock<IAmazonS3>();
        s3.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PutObjectResponse());
        s3.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new DeleteObjectResponse());
        var notif = new Mock<INotificacionService>();
        notif.Setup(x => x.NotificarComercioAsync(It.IsAny<Pedido>(), It.IsAny<TipoNotificacionPedido>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new IOException("Notificaciones no disponibles"));
        var env = new Mock<IWebHostEnvironment>(); env.SetupGet(x => x.EnvironmentName).Returns("Development");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("id", "5")], "test")) };
        var service = new ComprobantePagoService(repo.Object, new JwtContext(new HttpContextAccessor { HttpContext = context }),
            s3.Object, env.Object, new ConfigurationBuilder().Build(), notif.Object, NullLogger<ComprobantePagoService>.Instance);
        var response = await service.SubirAsync(uuid, new SubirComprobanteTransferenciaDto { ArchivoBase64 = "data:application/pdf;base64,JVBERi0xLjQ=" });
        Assert.Equal(codigo, response.Codigo);
        s3.Verify(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(borrados));
    }
}
