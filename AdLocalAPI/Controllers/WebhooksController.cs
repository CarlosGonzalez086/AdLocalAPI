using System.IO;
using System.Threading.Tasks;
using AdLocalAPI.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdLocalAPI.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : ApiControllerBase
    {
        private readonly IWebhookService _webhookService;

        public WebhooksController(IWebhookService webhookService)
        {
            _webhookService = webhookService;
        }

        [HttpPost("stripe")]
        public async Task<IActionResult> Handle(CancellationToken cancellationToken = default)
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);
            var signature = Request.Headers["Stripe-Signature"].ToString();

            var response = await _webhookService.ProcesarWebhookStripeAsync(json, signature, cancellationToken);
            return Responder(response);
        }
    }
}
