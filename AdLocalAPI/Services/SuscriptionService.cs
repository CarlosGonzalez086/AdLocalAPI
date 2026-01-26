using AdLocalAPI.Data;
using AdLocalAPI.Helpers;
using AdLocalAPI.Models;
using AdLocalAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

public interface ISuscriptionService
{
    Task<ApiResponse<string>> ContratarConTarjetaGuardada(int planId, string StripePaymentMethodId, bool autoRenew);
    Task<ApiResponse<string>> CrearSesionStripe(int planId);
    Task<ApiResponse<string>> GenerarReferenciaTransferencia(int planId,string banco);

    Task<AdLocalAPI.Models.Plan> ObtenerPlan(int planId);
    Task<ApiResponse<string>> CancelarPlan();
}

public class SuscriptionService : ISuscriptionService
{
    private readonly ISuscriptionRepository _repo;
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly UsuarioRepository _usuarioRepository;
    private readonly JwtContext _jwtContext;
    private readonly IWebHostEnvironment _env;

    public SuscriptionService(ISuscriptionRepository repo, AppDbContext context, IConfiguration config,JwtContext jwtContext, UsuarioRepository usuarioRepository,IWebHostEnvironment env)
    {
        _repo = repo;
        _context = context;
        _config = config;
        _jwtContext = jwtContext;
        _usuarioRepository = usuarioRepository;
        _env = env;
    }

    public async Task<AdLocalAPI.Models.Plan?> ObtenerPlan(int planId)
    {
        return await _context.Plans.FirstOrDefaultAsync(p => p.Id == planId && p.Activo);
    }

    public async Task<ApiResponse<string>> ContratarConTarjetaGuardada(int planId, string StripePaymentMethodId, bool autoRenew)
    {
        var plan = await ObtenerPlan(planId);
        if (plan == null) return ApiResponse<string>.Error("404", "Plan no encontrado");

        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

        try
        {
            int usuarioId = _jwtContext.GetUserId();

            var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
            if (usuario == null)
                return ApiResponse<string>.Error("404", "Usuario no encontrado");

            // Crear PaymentIntent
            var paymentService = new PaymentIntentService();
            var paymentIntent = await paymentService.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = (long)(plan.Precio * 100),
                Currency = "mxn",
                Customer = usuario.StripeCustomerId,
                PaymentMethod = StripePaymentMethodId,
                Metadata = new Dictionary<string, string>
                {
                    { "usuarioId", usuarioId.ToString() },
                    { "planId", plan.Id.ToString() },
                    { "plan_nombre", plan.Nombre },
                    { "autoRenew", autoRenew ? "si" : "no" },
                },

                OffSession = true,
                Confirm = true,
                PaymentMethodTypes = new List<string> { "card" },                
            });

            return ApiResponse<string>.Success(paymentIntent.Id, "Pago exitoso con tarjeta guardada");
        }
        catch (StripeException ex)
        {
            return ApiResponse<string>.Error("400", ex.Message);
        }
    }

    public async Task<ApiResponse<string>> CrearSesionStripe(int planId)
    {

        var plan = await ObtenerPlan(planId);
        if (plan == null) return ApiResponse<string>.Error("404", "Plan no encontrado");
        int usuarioId = _jwtContext.GetUserId();

        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario == null)
            return ApiResponse<string>.Error("404", "Usuario no encontrado");

        if (string.IsNullOrEmpty(usuario.StripeCustomerId))
        {
            var customerService = new Stripe.CustomerService();
            var customer = await customerService.CreateAsync(new Stripe.CustomerCreateOptions
            {
                Email = usuario.Email
            });

            usuario.StripeCustomerId = customer.Id;
            await _usuarioRepository.UpdateAsync(usuario);
        }

        string SuccessUrl = _env.IsProduction() ? "https://ad-local-gamma.vercel.app/app/checkout/success" : "http://localhost:5173/app/checkout/success";
        string CancelUrl = _env.IsProduction() ? "https://ad-local-gamma.vercel.app/app/checkout/cancel" : "http://localhost:5173/app/checkout/cancel";

        var options = new SessionCreateOptions
        {
            Locale = "es", // 👈 fuerza español
            PaymentMethodTypes = new List<string> { "card" },
            Mode = "payment",
            Customer = usuario.StripeCustomerId,
            Metadata = new Dictionary<string, string>
            {
                { "usuarioId", usuarioId.ToString() },
                { "planId", plan.Id.ToString() },
                { "plan_nombre", plan.Nombre }
            },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(plan.Precio * 100),
                        Currency = "mxn",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = plan.Nombre
                        }
                    },
                    Quantity = 1
                }
            },
            SuccessUrl = SuccessUrl,
            CancelUrl = CancelUrl,
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return ApiResponse<string>.Success(session.Url!, "Sesión creada");
    }

    public async Task<ApiResponse<string>> GenerarReferenciaTransferencia(
        int planId,
        string banco // "spei" | "oxxo"
    )
    {
        int usuarioId = _jwtContext.GetUserId();

        // 🔹 Obtener plan
        var plan = await ObtenerPlan(planId);
        if (plan == null)
            return ApiResponse<string>.Error("404", "Plan no encontrado");

        // 🔹 Obtener usuario
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario == null)
            return ApiResponse<string>.Error("404", "Usuario no encontrado");

        // 🔹 Validar método
        if (banco != "spei" && banco != "oxxo")
            return ApiResponse<string>.Error("400", "Método de pago no válido");

        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

        // 🔹 Crear customer en Stripe si no existe
        if (string.IsNullOrEmpty(usuario.StripeCustomerId))
        {
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = usuario.Email,
                Name = usuario.Nombre
            });

            usuario.StripeCustomerId = customer.Id;
            await _usuarioRepository.UpdateAsync(usuario);
        }

        // 🔹 URLs
        string successUrl = _env.IsProduction()
            ? "https://ad-local-gamma.vercel.app/app/checkout/success"
            : "http://localhost:5173/app/checkout/success";

        string cancelUrl = _env.IsProduction()
            ? "https://ad-local-gamma.vercel.app/app/checkout/cancel"
            : "http://localhost:5173/app/checkout/cancel";

        // 🔹 Base de Checkout Session
        var options = new SessionCreateOptions
        {
            Locale = "es", 
            Mode = "payment",
            Customer = usuario.StripeCustomerId,
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                Quantity = 1,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "mxn",
                    UnitAmount = (long)(plan.Precio * 100),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = plan.Nombre
                    }
                }
            }
        },
            Metadata = new Dictionary<string, string>
        {
            { "usuarioId", usuarioId.ToString() },
            { "planId", planId.ToString() },
            { "metodo", banco }
        }
        };

        // ============================
        // 🏦 SPEI (Transferencia)
        // ============================
        if (banco == "spei")
        {
            options.PaymentMethodTypes = new List<string>
        {
            "customer_balance"
        };

            options.PaymentMethodOptions = new SessionPaymentMethodOptionsOptions
            {
                CustomerBalance = new SessionPaymentMethodOptionsCustomerBalanceOptions
                {
                    FundingType = "bank_transfer",
                    BankTransfer = new SessionPaymentMethodOptionsCustomerBalanceBankTransferOptions
                    {
                        Type = "mx_bank_transfer"
                    }
                }
            };
        }

        // ============================
        // 🧾 OXXO
        // ============================
        if (banco == "oxxo")
        {
            options.PaymentMethodTypes = new List<string>
        {
            "oxxo"
        };
        }

        // 🔹 Crear sesión
        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return ApiResponse<string>.Success(
            "ok",
            session.Url // 👉 URL de Stripe Checkout
        );
    }


    public async Task<ApiResponse<string>> CancelarPlan()
    {
        int usuarioId = _jwtContext.GetUserId();

        var suscripcion = await _context.Suscripcions
            .Where(s => s.UsuarioId == usuarioId && s.Activa)
            .OrderByDescending(s => s.FechaFin)
            .FirstOrDefaultAsync();

        if (suscripcion == null)
            return ApiResponse<string>.Error("404", "No tienes una suscripción activa");

        suscripcion.Estado = "cancelada";
        suscripcion.Activa = false;
        suscripcion.AutoRenovacion = false;
        suscripcion.FechaCancelacion = DateTime.UtcNow;

        await _repo.UpdateAsync(suscripcion);

        return ApiResponse<string>.Success(
            "ok",
            $"Tu plan seguirá activo hasta {suscripcion.FechaFin:dd/MM/yyyy}"
        );
    }
}
