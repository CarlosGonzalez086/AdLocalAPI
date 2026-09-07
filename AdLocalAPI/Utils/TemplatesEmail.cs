namespace AdLocalAPI.Utils
{
    public static class TemplatesEmail
    {
        // =========================================================================
        // PALETA DE MARCA CORPORATIVA ADLOCAL
        // =========================================================================
        private const string BrandPrimary = "#6F4E37";         // Café tostado y tierra
        private const string BrandPrimaryDark = "#5A3E2B";     // Tostado oscuro
        private const string BrandPrimaryLight = "#C8A97E";    // Arena tostada
        private const string BrandPrimarySubtle = "#F5EFE6";   // Fondo crema sutil

        private const string BrandBotanical = "#00A85A";       // Verde botánico fresco
        private const string BrandBotanicalDark = "#008746";   // Verde orgánico profundo
        private const string BrandBotanicalLight = "#2CD483";  // Verde claro
        private const string BrandBotanicalSubtle = "#EBF7F0"; // Verde sutil suave

        private const string BackgroundWarm = "#FAF8F5";       // Papel lino / luz natural
        private const string SurfaceCream = "#F5EFE6";         // Crema superficie
        private const string SurfaceLinen = "#EFE8DC";         // Borde / lino superficie
        private const string TextPrimary = "#2D2520";          // Texto principal
        private const string TextSecondary = "#5C524B";        // Texto secundario
        private const string TextMuted = "#8C827A";            // Texto atenuado

        private const string DefaultAppUrl = "https://adlocal.jcarlosgonzalez086.workers.dev";

        // =========================================================================
        // 1. PLANTILLA: BIENVENIDA CLIENTE
        // =========================================================================
        public static string PlantillaBienvenidaCliente(string nombre, string? urlExplorar = null)
        {
            var targetUrl = string.IsNullOrWhiteSpace(urlExplorar) ? $"{DefaultAppUrl}/explorar" : urlExplorar;
            var nombreLimpio = string.IsNullOrWhiteSpace(nombre) ? "Estimado(a)" : nombre.Trim();

            var contenido = $@"
              <p style='margin: 0 0 16px 0; font-size: 15px; line-height: 1.6; color: {TextPrimary};'>
                Hola <strong style='color: {BrandPrimaryDark};'>{nombreLimpio}</strong>, tu cuenta ha sido creada exitosamente en <strong>AdLocal</strong>.
              </p>
              <p style='margin: 0 0 24px 0; font-size: 15px; line-height: 1.6; color: {TextSecondary};'>
                Estamos felices de que formes parte de nuestra comunidad. AdLocal es el punto de encuentro digital para descubrir comercios de tu zona, solicitar cotizaciones y conectar directamente con negocios locales sin fricciones.
              </p>

              <!-- Tarjeta de Beneficios -->
              <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: {SurfaceCream}; border: 1px solid {SurfaceLinen}; border-radius: 14px; margin-bottom: 28px;'>
                <tr>
                  <td style='padding: 22px 20px;'>
                    <div style='margin-bottom: 16px;'>
                      <table role='presentation' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                          <td valign='top' style='width: 28px;'>
                            <span style='display: inline-block; width: 22px; height: 22px; background-color: {BrandBotanical}; color: #ffffff; border-radius: 50%; text-align: center; line-height: 22px; font-size: 12px; font-weight: 700;'>✓</span>
                          </td>
                          <td style='padding-left: 10px;'>
                            <strong style='font-size: 14px; color: {BrandPrimaryDark};'>Comercios y servicios cercanos</strong>
                            <p style='margin: 2px 0 0 0; font-size: 13px; color: {TextSecondary}; line-height: 1.4;'>Encuentra catálogos, horarios de atención y ubicaciones exactas en tiempo real.</p>
                          </td>
                        </tr>
                      </table>
                    </div>
                    <div style='margin-bottom: 16px;'>
                      <table role='presentation' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                          <td valign='top' style='width: 28px;'>
                            <span style='display: inline-block; width: 22px; height: 22px; background-color: {BrandBotanical}; color: #ffffff; border-radius: 50%; text-align: center; line-height: 22px; font-size: 12px; font-weight: 700;'>✓</span>
                          </td>
                          <td style='padding-left: 10px;'>
                            <strong style='font-size: 14px; color: {BrandPrimaryDark};'>Cotizaciones y trato directo</strong>
                            <p style='margin: 2px 0 0 0; font-size: 13px; color: {TextSecondary}; line-height: 1.4;'>Comunícate de tú a tú con el propietario y resuelve tus necesidades al instante.</p>
                          </td>
                        </tr>
                      </table>
                    </div>
                    <div>
                      <table role='presentation' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                          <td valign='top' style='width: 28px;'>
                            <span style='display: inline-block; width: 22px; height: 22px; background-color: {BrandBotanical}; color: #ffffff; border-radius: 50%; text-align: center; line-height: 22px; font-size: 12px; font-weight: 700;'>✓</span>
                          </td>
                          <td style='padding-left: 10px;'>
                            <strong style='font-size: 14px; color: {BrandPrimaryDark};'>Comunidad transparente</strong>
                            <p style='margin: 2px 0 0 0; font-size: 13px; color: {TextSecondary}; line-height: 1.4;'>Revisa opiniones verificadas, calificaciones y apoya el comercio de tu localidad.</p>
                          </td>
                        </tr>
                      </table>
                    </div>
                  </td>
                </tr>
              </table>

              <!-- Botón CTA -->
              <div style='text-align: center; margin: 30px 0 10px 0;'>
                <a href='{targetUrl}' style='display: inline-block; background-color: {BrandBotanical}; color: #ffffff; font-size: 15px; font-weight: 600; text-decoration: none; padding: 14px 38px; border-radius: 12px; box-shadow: 0 4px 14px rgba(0, 168, 90, 0.28);'>
                  Explorar Comercios Locales
                </a>
              </div>";

            return ConstruirLayout(
                badgeTexto: "COMUNIDAD LOCAL",
                badgeBg: BrandBotanicalSubtle,
                badgeColor: BrandBotanicalDark,
                titulo: $"¡Bienvenido a AdLocal, {nombreLimpio}!",
                contenidoHtml: contenido
            );
        }

        // =========================================================================
        // 2. PLANTILLA: BIENVENIDA COMERCIO
        // =========================================================================
        public static string PlantillaBienvenidaComercio(string nombreComercio, string nombreContacto, string? urlPanel = null)
        {
            var targetUrl = string.IsNullOrWhiteSpace(urlPanel) ? $"{DefaultAppUrl}/comercio" : urlPanel;
            var comercio = string.IsNullOrWhiteSpace(nombreComercio) ? "tu negocio" : nombreComercio.Trim();
            var contacto = string.IsNullOrWhiteSpace(nombreContacto) ? "Estimado(a)" : nombreContacto.Trim();

            var contenido = $@"
              <p style='margin: 0 0 16px 0; font-size: 15px; line-height: 1.6; color: {TextPrimary};'>
                Hola <strong style='color: {BrandPrimaryDark};'>{contacto}</strong>, te damos la más cordial bienvenida a la red comercial de <strong>AdLocal</strong>.
              </p>
              <p style='margin: 0 0 24px 0; font-size: 15px; line-height: 1.6; color: {TextSecondary};'>
                El registro para <strong style='color: {BrandPrimaryDark};'>{comercio}</strong> ha sido completado. Nuestra misión es darte las herramientas tecnológicas necesarias para que tu negocio crezca, gane presencia digital y atraiga clientes locales todos los días.
              </p>

              <!-- Pasos recomendados -->
              <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: {SurfaceCream}; border: 1px solid {SurfaceLinen}; border-radius: 14px; margin-bottom: 28px;'>
                <tr>
                  <td style='padding: 22px 20px;'>
                    <div style='margin-bottom: 16px;'>
                      <table role='presentation' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                          <td valign='top' style='width: 28px;'>
                            <span style='display: inline-block; width: 22px; height: 22px; background-color: {BrandPrimary}; color: #ffffff; border-radius: 50%; text-align: center; line-height: 22px; font-size: 12px; font-weight: 700;'>1</span>
                          </td>
                          <td style='padding-left: 10px;'>
                            <strong style='font-size: 14px; color: {BrandPrimaryDark};'>Personaliza tu vitrina digital</strong>
                            <p style='margin: 2px 0 0 0; font-size: 13px; color: {TextSecondary}; line-height: 1.4;'>Configura tu logotipo, descripción, dirección exacta y horarios para generar confianza.</p>
                          </td>
                        </tr>
                      </table>
                    </div>
                    <div style='margin-bottom: 16px;'>
                      <table role='presentation' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                          <td valign='top' style='width: 28px;'>
                            <span style='display: inline-block; width: 22px; height: 22px; background-color: {BrandPrimary}; color: #ffffff; border-radius: 50%; text-align: center; line-height: 22px; font-size: 12px; font-weight: 700;'>2</span>
                          </td>
                          <td style='padding-left: 10px;'>
                            <strong style='font-size: 14px; color: {BrandPrimaryDark};'>Publica tus productos o servicios</strong>
                            <p style='margin: 2px 0 0 0; font-size: 13px; color: {TextSecondary}; line-height: 1.4;'>Sube imágenes de buena calidad, descripciones detalladas y precios actualizados.</p>
                          </td>
                        </tr>
                      </table>
                    </div>
                    <div>
                      <table role='presentation' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                          <td valign='top' style='width: 28px;'>
                            <span style='display: inline-block; width: 22px; height: 22px; background-color: {BrandPrimary}; color: #ffffff; border-radius: 50%; text-align: center; line-height: 22px; font-size: 12px; font-weight: 700;'>3</span>
                          </td>
                          <td style='padding-left: 10px;'>
                            <strong style='font-size: 14px; color: {BrandPrimaryDark};'>Atiende solicitudes y cotizaciones</strong>
                            <p style='margin: 2px 0 0 0; font-size: 13px; color: {TextSecondary}; line-height: 1.4;'>Monitorea pedidos, visualizaciones de tu perfil y responde con rapidez a tus clientes.</p>
                          </td>
                        </tr>
                      </table>
                    </div>
                  </td>
                </tr>
              </table>

              <!-- Botón CTA -->
              <div style='text-align: center; margin: 30px 0 10px 0;'>
                <a href='{targetUrl}' style='display: inline-block; background-color: {BrandPrimary}; color: #ffffff; font-size: 15px; font-weight: 600; text-decoration: none; padding: 14px 38px; border-radius: 12px; box-shadow: 0 4px 14px rgba(111, 78, 55, 0.30);'>
                  Ir a mi Panel de Comercio
                </a>
              </div>";

            return ConstruirLayout(
                badgeTexto: "COMERCIO ALIADO",
                badgeBg: BrandPrimarySubtle,
                badgeColor: BrandPrimaryDark,
                titulo: $"¡Bienvenido a AdLocal, {comercio}!",
                contenidoHtml: contenido
            );
        }

        // =========================================================================
        // 3. PLANTILLA: RECUPERACIÓN CON ENLACE (COMERCIO / ADMIN / WEB)
        // =========================================================================
        public static string PlantillaRecuperacionPasswordLink(string nombre, string codigo, string linkRestablecer)
        {
            var nombreLimpio = string.IsNullOrWhiteSpace(nombre) ? "Usuario" : nombre.Trim();

            var contenido = $@"
              <p style='margin: 0 0 16px 0; font-size: 15px; line-height: 1.6; color: {TextPrimary};'>
                Hola <strong style='color: {BrandPrimaryDark};'>{nombreLimpio}</strong>,
              </p>
              <p style='margin: 0 0 24px 0; font-size: 15px; line-height: 1.6; color: {TextSecondary};'>
                Recibimos una solicitud para restablecer la contraseña de acceso a tu cuenta en <strong>AdLocal</strong>. Para continuar, haz clic en el siguiente botón seguro:
              </p>

              <!-- Botón Principal -->
              <div style='text-align: center; margin: 28px 0;'>
                <a href='{linkRestablecer}' style='display: inline-block; background-color: {BrandPrimary}; color: #ffffff; font-size: 15px; font-weight: 600; text-decoration: none; padding: 14px 38px; border-radius: 12px; box-shadow: 0 4px 14px rgba(111, 78, 55, 0.30);'>
                  Restablecer mi Contraseña
                </a>
              </div>

              <!-- Código alternativo -->
              <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: {BackgroundWarm}; border: 1px dashed {BrandPrimaryLight}; border-radius: 12px; margin-bottom: 24px;'>
                <tr>
                  <td align='center' style='padding: 18px 20px;'>
                    <span style='font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 1.5px; color: {TextMuted};'>
                      O ingresa este código en la plataforma
                    </span>
                    <div style='font-size: 32px; font-weight: 800; letter-spacing: 8px; color: {BrandPrimaryDark}; margin-top: 8px; font-family: ""Inter"", monospace;'>
                      {codigo}
                    </div>
                  </td>
                </tr>
              </table>

              <!-- Aviso de seguridad -->
              <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: {BackgroundWarm}; border-left: 3px solid {BrandBotanical}; border-radius: 0 8px 8px 0;'>
                <tr>
                  <td style='padding: 12px 16px;'>
                    <p style='margin: 0; font-size: 12px; color: {TextSecondary}; line-height: 1.5;'>
                      🛡️ <strong>Aviso de seguridad:</strong> Este enlace y código son personales y expiran en <strong>15 minutos</strong>. Si tú no realizaste esta solicitud, puedes ignorar este correo sin que tu cuenta se vea afectada.
                    </p>
                  </td>
                </tr>
              </table>";

            return ConstruirLayout(
                badgeTexto: "SEGURIDAD DE CUENTA",
                badgeBg: BrandPrimarySubtle,
                badgeColor: BrandPrimaryDark,
                titulo: "Restablece tu contraseña",
                contenidoHtml: contenido
            );
        }

        // =========================================================================
        // 4. PLANTILLA: RECUPERACIÓN CON CÓDIGO (CLIENTE / APP MÓVIL)
        // =========================================================================
        public static string PlantillaRecuperacionPasswordCodigo(string nombre, string codigo)
        {
            var nombreLimpio = string.IsNullOrWhiteSpace(nombre) ? "Cliente" : nombre.Trim();

            var contenido = $@"
              <p style='margin: 0 0 16px 0; font-size: 15px; line-height: 1.6; color: {TextPrimary};'>
                Hola <strong style='color: {BrandPrimaryDark};'>{nombreLimpio}</strong>,
              </p>
              <p style='margin: 0 0 24px 0; font-size: 15px; line-height: 1.6; color: {TextSecondary};'>
                Recibimos una solicitud para cambiar la contraseña de tu cuenta en <strong>AdLocal</strong>. Introduce el siguiente código de verificación en la aplicación:
              </p>

              <!-- Tarjeta de Código de 6 dígitos -->
              <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: {SurfaceCream}; border: 1.5px solid {BrandPrimaryLight}; border-radius: 14px; margin-bottom: 24px;'>
                <tr>
                  <td align='center' style='padding: 26px 20px;'>
                    <span style='font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 1.5px; color: {BrandPrimary};'>
                      TU CÓDIGO DE RECUPERACIÓN
                    </span>
                    <div style='font-size: 38px; font-weight: 800; letter-spacing: 9px; color: {BrandPrimaryDark}; margin: 12px 0 6px 0; font-family: ""Inter"", monospace;'>
                      {codigo}
                    </div>
                    <span style='font-size: 12px; font-weight: 500; color: {TextMuted};'>
                      ⏱ Válido únicamente durante <strong>10 minutos</strong>
                    </span>
                  </td>
                </tr>
              </table>

              <!-- Aviso de seguridad -->
              <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: {BackgroundWarm}; border-left: 3px solid {BrandBotanical}; border-radius: 0 8px 8px 0;'>
                <tr>
                  <td style='padding: 12px 16px;'>
                    <p style='margin: 0; font-size: 12px; color: {TextSecondary}; line-height: 1.5;'>
                      🛡️ <strong>Importante:</strong> Nunca compartas este código con terceros. Ningún miembro del equipo de AdLocal te solicitará este código bajo ninguna circunstancia.
                    </p>
                  </td>
                </tr>
              </table>";

            return ConstruirLayout(
                badgeTexto: "CÓDIGO DE VERIFICACIÓN",
                badgeBg: BrandBotanicalSubtle,
                badgeColor: BrandBotanicalDark,
                titulo: "Código de recuperación de contraseña",
                contenidoHtml: contenido,
                notaPie: "Si no solicitaste este código, puedes ignorar este mensaje con total seguridad."
            );
        }

        // =========================================================================
        // 5. PLANTILLA: CONFIRMACIÓN DE CAMBIO DE CONTRASEÑA
        // =========================================================================
        public static string PlantillaConfirmacionCambioPassword(string nombre, string? fechaHora = null, string? loginUrl = null)
        {
            var nombreLimpio = string.IsNullOrWhiteSpace(nombre) ? "Usuario" : nombre.Trim();
            var fecha = string.IsNullOrWhiteSpace(fechaHora)
                ? DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm") + " UTC"
                : fechaHora;
            var targetLogin = string.IsNullOrWhiteSpace(loginUrl) ? $"{DefaultAppUrl}/login" : loginUrl;

            var contenido = $@"
              <p style='margin: 0 0 16px 0; font-size: 15px; line-height: 1.6; color: {TextPrimary};'>
                Hola <strong style='color: {BrandPrimaryDark};'>{nombreLimpio}</strong>,
              </p>
              <p style='margin: 0 0 22px 0; font-size: 15px; line-height: 1.6; color: {TextSecondary};'>
                Te notificamos que la contraseña de tu cuenta en <strong>AdLocal</strong> ha sido actualizada exitosamente.
              </p>

              <!-- Tarjeta de Estado -->
              <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: {SurfaceCream}; border: 1px solid {SurfaceLinen}; border-radius: 14px; margin-bottom: 24px;'>
                <tr>
                  <td style='padding: 20px;'>
                    <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0'>
                      <tr>
                        <td style='font-size: 13px; color: {TextMuted}; padding: 6px 0;'>Fecha y hora:</td>
                        <td align='right' style='font-size: 13px; font-weight: 600; color: {BrandPrimaryDark}; padding: 6px 0;'>{fecha}</td>
                      </tr>
                      <tr>
                        <td style='font-size: 13px; color: {TextMuted}; padding: 6px 0;'>Seguridad de la cuenta:</td>
                        <td align='right' style='font-size: 13px; font-weight: 700; color: {BrandBotanical}; padding: 6px 0;'>✔ Actualizada correctamente</td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>

              <!-- Aviso de Alerta si no fue el usuario -->
              <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: {BackgroundWarm}; border-left: 3px solid {BrandPrimaryLight}; border-radius: 0 8px 8px 0; margin-bottom: 28px;'>
                <tr>
                  <td style='padding: 14px 16px;'>
                    <p style='margin: 0; font-size: 13px; color: {TextSecondary}; line-height: 1.5;'>
                      ⚠️ <strong>¿No fuiste tú?</strong> Si no realizaste esta acción, es posible que alguien más tenga acceso a tu correo. Te sugerimos restablecer tu contraseña inmediatamente y contactar a nuestro equipo de soporte.
                    </p>
                  </td>
                </tr>
              </table>

              <!-- Botón Iniciar Sesión -->
              <div style='text-align: center; margin: 20px 0 10px 0;'>
                <a href='{targetLogin}' style='display: inline-block; background-color: {BrandPrimary}; color: #ffffff; font-size: 15px; font-weight: 600; text-decoration: none; padding: 13px 36px; border-radius: 12px; box-shadow: 0 4px 14px rgba(111, 78, 55, 0.30);'>
                  Iniciar Sesión
                </a>
              </div>";

            return ConstruirLayout(
                badgeTexto: "SEGURIDAD DE CUENTA",
                badgeBg: BrandBotanicalSubtle,
                badgeColor: BrandBotanicalDark,
                titulo: "Contraseña actualizada exitosamente",
                contenidoHtml: contenido
            );
        }

        // =========================================================================
        // 6. PLANTILLA: BIENVENIDA COLABORADOR
        // =========================================================================
        public static string PlantillaCorreoBienvenidaColaborador(
            string nombre,
            string correo,
            string codigo,
            string linkCrearPassword
        )
        {
            var nombreLimpio = string.IsNullOrWhiteSpace(nombre) ? "Colaborador" : nombre.Trim();

            var contenido = $@"
              <p style='margin: 0 0 16px 0; font-size: 15px; line-height: 1.6; color: {TextPrimary};'>
                Hola <strong style='color: {BrandPrimaryDark};'>{nombreLimpio}</strong>,
              </p>
              <p style='margin: 0 0 20px 0; font-size: 15px; line-height: 1.6; color: {TextSecondary};'>
                Has sido registrado como colaborador en <strong>AdLocal</strong> con el correo <strong>{correo}</strong>. Para comenzar a operar en la plataforma, crea tu contraseña de acceso:
              </p>

              <!-- Código de Acceso -->
              <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: {SurfaceCream}; border: 1.5px solid {BrandPrimaryLight}; border-radius: 14px; margin-bottom: 24px;'>
                <tr>
                  <td align='center' style='padding: 22px 20px;'>
                    <span style='font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 1.5px; color: {BrandPrimary};'>
                      TU CÓDIGO DE ACTIVACIÓN
                    </span>
                    <div style='font-size: 34px; font-weight: 800; letter-spacing: 8px; color: {BrandPrimaryDark}; margin: 10px 0; font-family: ""Inter"", monospace;'>
                      {codigo}
                    </div>
                  </td>
                </tr>
              </table>

              <!-- Botón Crear Contraseña -->
              <div style='text-align: center; margin: 26px 0;'>
                <a href='{linkCrearPassword}' style='display: inline-block; background-color: {BrandPrimary}; color: #ffffff; font-size: 15px; font-weight: 600; text-decoration: none; padding: 14px 38px; border-radius: 12px; box-shadow: 0 4px 14px rgba(111, 78, 55, 0.30);'>
                  Crear mi Contraseña
                </a>
              </div>";

            return ConstruirLayout(
                badgeTexto: "EQUIPO DE TRABAJO",
                badgeBg: BrandPrimarySubtle,
                badgeColor: BrandPrimaryDark,
                titulo: "Bienvenido al equipo en AdLocal",
                contenidoHtml: contenido,
                notaPie: "Este enlace y código son confidenciales. Si no reconoces esta invitación, ignora este mensaje."
            );
        }

        // =========================================================================
        // 7. COMPATIBILIDAD CON CÓDIGO LEGADO
        // =========================================================================
        public static string PlantillaCorreoCambioPasswordCoffee(string codigo, string link)
        {
            return PlantillaRecuperacionPasswordLink("Usuario", codigo, link);
        }

        // =========================================================================
        // 8. PLANTILLA: PRUEBA DE CONFIGURACIÓN SMTP
        // =========================================================================
        public static string PlantillaPruebaConfiguracion(string emailDestino)
        {
            var fecha = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss") + " UTC";

            var contenido = $@"
              <p style='margin: 0 0 16px 0; font-size: 15px; line-height: 1.6; color: {TextPrimary};'>
                Hola,
              </p>
              <p style='margin: 0 0 20px 0; font-size: 15px; line-height: 1.6; color: {TextSecondary};'>
                Este mensaje confirma que el servicio de correo electrónico de <strong>AdLocal</strong> está operando correctamente con los parámetros SMTP configurados en la base de datos.
              </p>

              <!-- Tarjeta de Diagnóstico -->
              <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: {SurfaceCream}; border: 1px solid {SurfaceLinen}; border-radius: 14px; margin-bottom: 24px;'>
                <tr>
                  <td style='padding: 20px;'>
                    <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0'>
                      <tr>
                        <td style='font-size: 13px; color: {TextMuted}; padding: 6px 0;'>Destinatario de prueba:</td>
                        <td align='right' style='font-size: 13px; font-weight: 600; color: {BrandPrimaryDark}; padding: 6px 0;'>{emailDestino}</td>
                      </tr>
                      <tr>
                        <td style='font-size: 13px; color: {TextMuted}; padding: 6px 0;'>Fecha y hora:</td>
                        <td align='right' style='font-size: 13px; font-weight: 600; color: {BrandPrimaryDark}; padding: 6px 0;'>{fecha}</td>
                      </tr>
                      <tr>
                        <td style='font-size: 13px; color: {TextMuted}; padding: 6px 0;'>Estado de conexión:</td>
                        <td align='right' style='font-size: 13px; font-weight: 700; color: {BrandBotanical}; padding: 6px 0;'>✔ Conexión y envío exitoso</td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>

              <p style='margin: 0; font-size: 13px; color: {TextSecondary}; line-height: 1.5;'>
                Todos los flujos de bienvenida, recuperación y confirmación de contraseñas están listos para enviar correos con esta misma identidad corporativa.
              </p>";

            return ConstruirLayout(
                badgeTexto: "DIAGNÓSTICO DEL SISTEMA",
                badgeBg: BrandBotanicalSubtle,
                badgeColor: BrandBotanicalDark,
                titulo: "Configuración SMTP verificada",
                contenidoHtml: contenido
            );
        }

        // =========================================================================
        // MOTOR DE LAYOUT CORPORATIVO (INTER + IDENTIDAD VISUAL ADLOCAL)
        // =========================================================================
        private static string ConstruirLayout(
            string badgeTexto,
            string badgeBg,
            string badgeColor,
            string titulo,
            string contenidoHtml,
            string? notaPie = null)
        {
            var anoActual = DateTime.UtcNow.Year;
            var seccionNotaPie = string.IsNullOrWhiteSpace(notaPie)
                ? string.Empty
                : $@"
                  <tr>
                    <td align='center' style='padding: 0 32px 20px 32px;'>
                      <p style='margin: 0; font-size: 12px; line-height: 1.5; color: {TextMuted};'>
                        {notaPie}
                      </p>
                    </td>
                  </tr>";

            return $@"<!DOCTYPE html>
<html lang='es'>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <meta http-equiv='X-UA-Compatible' content='IE=edge'>
  <title>{titulo}</title>
  <style>
    @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap');
    body, table, td, p, a {{ -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; }}
    table, td {{ mso-table-lspace: 0pt; mso-table-rspace: 0pt; }}
    body {{
      margin: 0 !important;
      padding: 0 !important;
      width: 100% !important;
      background-color: {BackgroundWarm};
      font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    }}
  </style>
</head>
<body style='margin: 0; padding: 0; background-color: {BackgroundWarm}; font-family: ""Inter"", -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;'>

  <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: {BackgroundWarm}; width: 100%;'>
    <tr>
      <td align='center' style='padding: 40px 16px;'>

        <!-- Tarjeta Central Contenedora -->
        <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='max-width: 580px; background-color: #ffffff; border-radius: 16px; border: 1px solid {SurfaceLinen}; box-shadow: 0 4px 20px rgba(111, 78, 55, 0.06); overflow: hidden;'>

          <!-- Franja Superior de Marca -->
          <tr>
            <td height='5' style='background: linear-gradient(90deg, {BrandPrimary} 0%, {BrandPrimaryLight} 50%, {BrandBotanical} 100%); font-size: 0; line-height: 0;'>&nbsp;</td>
          </tr>

          <!-- Cabecera / Identidad Corporativa -->
          <tr>
            <td align='center' style='padding: 32px 32px 18px 32px;'>
              <table role='presentation' border='0' cellspacing='0' cellpadding='0'>
                <tr>
                  <td align='center'>
                    <div style='display: inline-block;'>
                      <span style='font-size: 26px; font-weight: 800; letter-spacing: 2px; color: {BrandPrimary}; vertical-align: middle;'>AD</span><span style='font-size: 26px; font-weight: 800; letter-spacing: 2px; color: {BrandBotanical}; vertical-align: middle;'>LOCAL</span>
                    </div>
                    <div style='font-size: 10px; font-weight: 700; letter-spacing: 2px; text-transform: uppercase; color: {BrandPrimaryLight}; margin-top: 4px;'>
                      COMERCIO LOCAL &bull; COMUNIDAD
                    </div>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Píldora de Categoría y Título -->
          <tr>
            <td align='center' style='padding: 0 32px 12px 32px;'>
              <div style='display: inline-block; background-color: {badgeBg}; color: {badgeColor}; font-size: 11px; font-weight: 700; letter-spacing: 1.2px; text-transform: uppercase; padding: 6px 14px; border-radius: 20px; margin-bottom: 14px;'>
                {badgeTexto}
              </div>
              <h1 style='margin: 0; font-size: 22px; font-weight: 700; color: {BrandPrimaryDark}; line-height: 1.35; letter-spacing: -0.3px;'>
                {titulo}
              </h1>
            </td>
          </tr>

          <!-- Cuerpo Principal -->
          <tr>
            <td style='padding: 12px 32px 32px 32px;'>
              {contenidoHtml}
            </td>
          </tr>

          {seccionNotaPie}

          <!-- Separador -->
          <tr>
            <td style='padding: 0 32px;'>
              <div style='border-top: 1px solid {SurfaceLinen};'></div>
            </td>
          </tr>

          <!-- Pie de Tarjeta -->
          <tr>
            <td align='center' style='padding: 22px 32px 24px 32px; background-color: {BackgroundWarm};'>
              <p style='margin: 0 0 6px 0; font-size: 12px; color: {TextMuted}; line-height: 1.5;'>
                ¿Tienes preguntas o necesitas asistencia? Contáctanos en <a href='mailto:soporte@adlocal.com' style='color: {BrandPrimary}; font-weight: 600; text-decoration: none;'>soporte@adlocal.com</a>
              </p>
              <p style='margin: 0; font-size: 11px; color: #A89F97;'>
                &copy; {anoActual} AdLocal. Todos los derechos reservados.
              </p>
            </td>
          </tr>

        </table>

        <!-- Nota legal fuera de tarjeta -->
        <table role='presentation' width='100%' border='0' cellspacing='0' cellpadding='0' style='max-width: 580px; margin-top: 16px;'>
          <tr>
            <td align='center' style='padding: 0 16px;'>
              <p style='margin: 0; font-size: 11px; color: #A89F97; line-height: 1.5;'>
                Este mensaje fue enviado de manera automática por AdLocal.<br>
                Por favor, no respondas directamente a este correo.
              </p>
            </td>
          </tr>
        </table>

      </td>
    </tr>
  </table>

</body>
</html>";
        }
    }
}
