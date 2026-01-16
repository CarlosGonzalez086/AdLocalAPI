namespace AdLocalAPI.Utils
{
    public class TemplatesEmail
    {
        public static string PlantillaCorreoCambioPasswordCoffee(string codigo, string link)
        {
            return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
  <title>Restablecer contraseña</title>
</head>

<body style='
  margin:0;
  padding:0;
  font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;
  background-color: #f5e9cf;
'>

  <table width='100%' cellpadding='0' cellspacing='0'>
    <tr>
      <td align='center' style='padding: 40px 16px;'>

        <!-- Card -->
        <table width='600' cellpadding='0' cellspacing='0' style='
          background-color: #ffffff;
          border-radius: 16px;
          box-shadow: 0 10px 30px rgba(0,0,0,0.08);
          padding: 32px;
        '>

          <!-- Header -->
          <tr>
            <td align='center' style='padding-bottom: 12px;'>
              <h1 style='
                margin: 0;
                font-size: 24px;
                font-weight: 700;
                color: #6F4E37;
              '>
                Restablecer contraseña
              </h1>
            </td>
          </tr>

          <tr>
            <td align='center' style='padding-bottom: 24px;'>
              <p style='
                margin: 0;
                font-size: 15px;
                line-height: 1.6;
                color: #4a4a4a;
              '>
                Recibimos una solicitud para cambiar tu contraseña.<br/>
                Usa el código o el botón de abajo.
              </p>
            </td>
          </tr>

          <!-- Código -->
          <tr>
            <td align='center' style='padding: 20px 0;'>
              <div style='
                background-color: #f5e9cf;
                border-radius: 12px;
                padding: 18px 24px;
                display: inline-block;
              '>
                <p style='
                  margin: 0;
                  font-size: 14px;
                  color: #6F4E37;
                '>
                  Tu código
                </p>
                <div style='
                  font-size: 32px;
                  font-weight: 700;
                  letter-spacing: 6px;
                  color: #e8692c;
                  margin-top: 6px;
                '>
                  {codigo}
                </div>
              </div>
            </td>
          </tr>

          <!-- Botón -->
          <tr>
            <td align='center' style='padding: 24px 0;'>
              <a href='{link}' style='
                background-color: #6F4E37;
                color: #ffffff;
                text-decoration: none;
                padding: 14px 32px;
                border-radius: 14px;
                font-size: 15px;
                font-weight: 600;
                display: inline-block;
                box-shadow: 0 6px 16px rgba(111, 78, 55, 0.35);
              '>
                Cambiar contraseña
              </a>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td align='center' style='padding-top: 16px;'>
              <p style='
                font-size: 13px;
                line-height: 1.6;
                color: #777;
                margin: 0;
              '>
                Este enlace expirará en <strong>15 minutos</strong>.<br/>
                Si no solicitaste este cambio, ignora este correo.
              </p>
            </td>
          </tr>

        </table>

        <!-- Branding -->
        <p style='
          margin-top: 20px;
          font-size: 12px;
          color: #999;
        '>
          © {DateTime.UtcNow.Year} AdLocal
        </p>

      </td>
    </tr>
  </table>

</body>
</html>";
        }

    }
}
