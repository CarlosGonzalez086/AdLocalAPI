using System;
using System.Collections.Generic;
using AdLocalAPI.DTOs;
using AdLocalAPI.DTOs.Direcciones;
using AdLocalAPI.DTOs.UsuarioCliente.Checkout;
using AdLocalAPI.Utils;
using AdLocalAPI.Validators;
using static AdLocalAPI.DTOs.PagosComercio;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class DtoValidatorsTests
    {
        [Fact]
        public void UsuarioRegistroDtoValidator_Valido_PasaValidacion()
        {
            var validator = new UsuarioRegistroDtoValidator();
            var dto = new UsuarioRegistroDto
            {
                Nombre = "Carlos Gonzalez",
                Email = "carlos@example.com",
                Password = "Password123!"
            };

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("", "carlos@example.com", "Password123!")]
        [InlineData("Carlos", "invalido-email", "Password123!")]
        [InlineData("Carlos", "carlos@example.com", "123")] // menor a 8 caracteres
        public void UsuarioRegistroDtoValidator_Invalido_FallaValidacion(string nombre, string email, string password)
        {
            var validator = new UsuarioRegistroDtoValidator();
            var dto = new UsuarioRegistroDto
            {
                Nombre = nombre,
                Email = email,
                Password = password
            };

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ClienteRegistroDtoValidator_Valido_PasaValidacion()
        {
            var validator = new ClienteRegistroDtoValidator();
            var dto = new ClienteRegistroDto
            {
                Nombre = "Maria Lopez",
                Email = "maria@example.com",
                Password = "SuperSecret123",
                ConfirmarPassword = "SuperSecret123"
            };

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ClienteRegistroDtoValidator_PasswordsNoCoinciden_FallaValidacion()
        {
            var validator = new ClienteRegistroDtoValidator();
            var dto = new ClienteRegistroDto
            {
                Nombre = "Maria Lopez",
                Email = "maria@example.com",
                Password = "SuperSecret123",
                ConfirmarPassword = "DiferentePassword456"
            };

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(ClienteRegistroDto.ConfirmarPassword));
        }

        [Fact]
        public void LoginDtoValidator_CamposVacios_FallaValidacion()
        {
            var validator = new LoginDtoValidator();
            var dto = new LoginDto
            {
                Email = "",
                Password = ""
            };

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginDto.Email));
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginDto.Password));
        }

        [Fact]
        public void CheckoutComercioDtoValidator_Valido_PasaValidacion()
        {
            var validator = new CheckoutComercioDtoValidator();
            var dto = new CheckoutComercioDto
            {
                ComercioUuid = Guid.NewGuid(),
                TipoEntrega = TipoEntregaPedido.Recoger,
                MetodoPago = MetodoPagoPedido.Efectivo,
                Observaciones = "Entregar en mostrador"
            };

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void CheckoutComercioDtoValidator_DomicilioSinDireccion_FallaValidacion()
        {
            var validator = new CheckoutComercioDtoValidator();
            var dto = new CheckoutComercioDto
            {
                ComercioUuid = Guid.NewGuid(),
                TipoEntrega = TipoEntregaPedido.Domicilio,
                MetodoPago = MetodoPagoPedido.Efectivo,
                DireccionUuid = null
            };

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CheckoutComercioDto.DireccionUuid));
        }

        [Fact]
        public void DireccionUsuarioDtoValidator_CodigoPostalYTelefonoValidos_Pasa()
        {
            var validator = new DireccionUsuarioDtoValidator();
            var dto = new DireccionUsuarioDto
            {
                Alias = "Casa",
                Calle = "Av. Insurgentes Sur",
                NumeroExterior = "123",
                Colonia = "Roma Sur",
                IdEstado = 1,
                IdMunicipio = 1,
                CodigoPostal = "06760",
                Telefono = "5512345678",
                Latitud = 19.4326m,
                Longitud = -99.1332m
            };

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("1234")] // 4 digitos
        [InlineData("0676a")] // contiene letra
        [InlineData("123456")] // 6 digitos
        public void DireccionUsuarioDtoValidator_CodigoPostalInvalido_Falla(string cpInvalido)
        {
            var validator = new DireccionUsuarioDtoValidator();
            var dto = new DireccionUsuarioDto
            {
                Alias = "Casa",
                Calle = "Av. Insurgentes Sur",
                NumeroExterior = "123",
                Colonia = "Roma Sur",
                IdEstado = 1,
                IdMunicipio = 1,
                CodigoPostal = cpInvalido
            };

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(DireccionUsuarioDto.CodigoPostal));
        }

        [Fact]
        public void HorarioComercioDtoValidator_HoraCierreMayorQueApertura_Pasa()
        {
            var validator = new HorarioComercioDtoValidator();
            var dto = new HorarioComercioDto
            {
                Dia = DayOfWeek.Monday,
                Abierto = true,
                HoraApertura = new TimeSpan(9, 0, 0),
                HoraCierre = new TimeSpan(18, 0, 0)
            };

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void HorarioComercioDtoValidator_HoraCierreMenorOIgualApertura_Falla()
        {
            var validator = new HorarioComercioDtoValidator();
            var dto = new HorarioComercioDto
            {
                Dia = DayOfWeek.Monday,
                Abierto = true,
                HoraApertura = new TimeSpan(18, 0, 0),
                HoraCierre = new TimeSpan(9, 0, 0)
            };

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(HorarioComercioDto.HoraCierre));
        }

        [Fact]
        public void CrearTarjetaDtoValidator_PrefijoPmStripe_Pasa()
        {
            var validator = new CrearTarjetaDtoValidator();
            var dto = new CrearTarjetaDto
            {
                PaymentMethodId = "pm_1Nx982abcXYZ1234567890"
            };

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void CrearTarjetaDtoValidator_SinPrefijoPm_Falla()
        {
            var validator = new CrearTarjetaDtoValidator();
            var dto = new CrearTarjetaDto
            {
                PaymentMethodId = "tok_1Nx982abcXYZ"
            };

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CrearTarjetaDto.PaymentMethodId));
        }

        [Fact]
        public void GuardarCuentaBancariaAdLocalDtoValidator_Clabe18Digitos_Pasa()
        {
            var validator = new GuardarCuentaBancariaAdLocalDtoValidator();
            var dto = new GuardarCuentaBancariaAdLocalDto
            {
                Banco = "BBVA",
                Beneficiario = "AdLocal S.A. de C.V.",
                Clabe = "012180001234567890"
            };

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("12345678901234567")] // 17 digitos
        [InlineData("1234567890123456789")] // 19 digitos
        [InlineData("01218000123456789A")] // contiene caracter no numerico
        public void GuardarCuentaBancariaAdLocalDtoValidator_ClabeInvalida_Falla(string clabeInvalida)
        {
            var validator = new GuardarCuentaBancariaAdLocalDtoValidator();
            var dto = new GuardarCuentaBancariaAdLocalDto
            {
                Banco = "BBVA",
                Beneficiario = "AdLocal",
                Clabe = clabeInvalida
            };

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(GuardarCuentaBancariaAdLocalDto.Clabe));
        }

        [Fact]
        public void CrearCotizacionDtoValidator_CamposCorrectos_Pasa()
        {
            var validator = new CrearCotizacionDtoValidator();
            var dto = new CrearCotizacionDto
            {
                ProductoUuid = Guid.NewGuid(),
                Solicitud = "Cotización para banquete de 50 personas."
            };

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void CalificacionComentarioCreateDtoValidator_Rango1a5_ValidaCorrectamente()
        {
            var validator = new CalificacionComentarioCreateDtoValidator();

            var valido = new CalificacionComentarioCreateDto
            {
                IdComercio = 1,
                NombrePersona = "Juan Perez",
                Calificacion = 5,
                Comentario = "Excelente servicio"
            };
            Assert.True(validator.Validate(valido).IsValid);

            var bajo = new CalificacionComentarioCreateDto
            {
                IdComercio = 1,
                NombrePersona = "Juan Perez",
                Calificacion = 0
            };
            Assert.False(validator.Validate(bajo).IsValid);

            var alto = new CalificacionComentarioCreateDto
            {
                IdComercio = 1,
                NombrePersona = "Juan Perez",
                Calificacion = 6
            };
            Assert.False(validator.Validate(alto).IsValid);
        }
    }
}
