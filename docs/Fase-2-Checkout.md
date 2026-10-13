# Fase 2 — Checkout recuperable

Estado al 12 de octubre de 2026: implementación y validación local completadas. Publicación y aceptación en staging/producción pendientes. Los cambios locales de la fase 1 se conservaron.

## Comportamiento implementado

- La web guarda una clave aleatoria y el payload confirmado antes de enviarlo. Se conserva por cuenta ante pérdida de conexión, respuesta incierta, recarga o cierre de la pestaña. Un fallo al guardar impide enviar la compra.
- El reintento reutiliza exactamente la operación guardada. Dos clics durante una petición comparten la misma promesa. La defensa contra duplicación entre pestañas está también en PostgreSQL.
- El API consulta el resultado confirmado antes de buscar un carrito activo. La recuperación devuelve los mismos UUID de pedidos incluso después de cerrar el carrito.
- La clave queda vinculada mediante SHA-256 al UUID y versión del carrito, comercios, pago, entrega, dirección y observaciones. Otra solicitud con la misma clave devuelve 409.
- La versión del carrito incorpora detalles, cantidades, observaciones, precios, control de stock, permisos de entrega y disponibilidad de productos/comercios. Se comprueba al confirmar y dentro de la transacción.
- Las validaciones previas no persisten `processing`. La reclamación de unicidad, stock, pedidos, detalles/historiales, cierre de carrito y resultado quedan en una transacción `Serializable`. Un fallo previo al commit revierte también el registro de idempotencia.
- Se reintentan hasta tres intentos transaccionales ante serialización, deadlock o colisión del índice de idempotencia. Las excepciones PostgreSQL pueden estar envueltas: se examina la cadena de causas. Los errores de conexión y resultados inciertos no se repiten automáticamente como si el rollback estuviera demostrado.
- Las escrituras de detalles comprueban y bloquean el carrito activo. Una escritura tardía no puede reabrir ni modificar el carrito cerrado.
- La web muestra recuperación aun sin carrito. Solo una validación 400 reconocida permite corregir el formulario automáticamente. Una respuesta incompleta conserva la operación. Para cerrar manualmente una comprobación, el usuario debe confirmar que revisó sus pedidos.

## Contrato

`GET /api/Checkout` añade `carritoUuid` y `carritoVersion`.

`POST /api/Checkout/confirmar` requiere esos valores y una clave explícita: `idempotencyKey` en el cuerpo o `Idempotency-Key` en el encabezado. Si se envían ambos deben coincidir. Se conserva `ApiResponse` y la autorización Cliente.

No existe un fallback que genere la clave a partir del carrito. Los clientes anteriores deberán actualizarse; recibirán 400 si les faltan los nuevos datos.

## Evidencia

| Comprobación | Resultado local |
|---|---|
| Suite API Release | 322/322; cero omitidas |
| PostgreSQL 17 + PostGIS, bases independientes por caso | 16/16; incluidas en las 322 |
| Suite web | 81/81 |
| Web Astro check | 143 archivos; cero errores y warnings; 16 hints de deprecaciones existentes |
| ESLint y build Astro/Vercel | Aprobados |
| Control específico de secretos rastreados y `git diff --check` | Aprobados |
| Migración idempotente sobre PostgreSQL sintético | Conserva filas; segunda ejecución sin duplicación |

Los casos PostgreSQL prueban reintento después de commit, corrección tras validación, fallo después de descontar stock, dos solicitudes con la misma clave, dos claves sobre el mismo carrito, dos clientes por el último stock, payload distinto, legado vencido, versión obsoleta, escritura tardía, contrato HTTP con JWT, cancelación antes de guardar pedidos, cambio de control de stock y comercio oculto.

La web tiene pruebas del almacenamiento, transporte del payload, consolidación de solicitudes y renderizado React de la recuperación. No equivalen a una prueba de interacción en un navegador/dispositivo real ni a una prueba de carga. El panel conserva los cambios de fase 1; su checkout de suscripciones Stripe pertenece a la fase 6.

## Repetir las pruebas

El workflow CI incorpora PostgreSQL/PostGIS mediante `postgis/postgis:17-3.5`, documentado por el [proyecto oficial](https://github.com/postgis/docker-postgis). El workflow se preparó localmente; su ejecución en GitHub todavía no está demostrada.

Para ejecutar las pruebas API localmente, usar exclusivamente una instancia PostgreSQL/PostGIS de prueba en loopback:

```powershell
$env:ADLOCAL_TEST_POSTGRES = 'Host=127.0.0.1;Port=55432;Database=postgres;Username=checkout_test;Pooling=false'
dotnet test AdLocalAPI.Tests/AdLocalAPI.Tests.csproj -c Release -p:UseAppHost=false
```

La instancia usada para esta entrega se creó desde cero, solo escuchaba en `127.0.0.1:55432` y usó datos sintéticos. No se leyó la conexión productiva. Cada caso crea y elimina únicamente su base aleatoria `adlocal_checkout_test_<guid>`; el runner rechaza hosts externos. Requiere permisos para crear bases y la extensión PostGIS instalada. Sin la variable, esos 16 casos se omiten explícitamente: una suite así no demuestra concurrencia real.

## Publicación: orden obligatorio

1. Completar los pendientes externos de fase 1. Identificar artefactos, API, web y base destino. Crear backup y tener el procedimiento de recuperación disponible.
2. Ejecutar las pruebas y el ensayo funcional en staging. Consultar `__EFMigrationsHistory`: la base debe tener las migraciones previas hasta `20260924032004_TokensRevocadosAntesDe`. El SQL de esta entrega solo contiene la nueva columna; no sustituye migraciones anteriores.
3. Detener temporalmente las confirmaciones y drenar las peticiones del API anterior. No ejecutar ambos escritores de checkout a la vez durante el cambio.
4. Aplicar `20261012211320_CheckoutRequestHash`. Revisar antes el SQL `scripts/checkout-request-hash.sql`: agrega exclusivamente `RequestHash`, admite reejecución por historial EF y no modifica pedidos ni saldos.
5. Publicar API y web actualizadas de forma coordinada. La migración debe existir ANTES de iniciar este API. Publicar solo la web contra el API anterior pierde las garantías nuevas; publicar solo el API obliga a actualizar los clientes antiguos.
6. Ensayar una compra sintética, pérdida de respuesta, recuperación después de recarga y dos pestañas. Verificar UUID, cantidades, stock, filas de idempotencia e inexistencia de duplicados. Confirmar también cuentas Cliente distintas y carrito nuevo después de resolver la compra.
7. Reabrir checkout solo después de esa aceptación. Vigilar 409/500 y contención transaccional; no asumir escalabilidad por las pruebas de dos solicitantes.

Para generar de nuevo únicamente el SQL de esta fase:

```powershell
# Proporcionar ConnectionStrings__DefaultConnection por entorno seguro.
dotnet ef migrations script 20260924032004_TokensRevocadosAntesDe 20261012211320_CheckoutRequestHash --project AdLocalAPI/AdLocalAPI.csproj --configuration Release --idempotent --output scripts/checkout-request-hash.sql
```

La factoría de diseño evita iniciar los proveedores del API y cargar configuraciones privadas al ejecutar herramientas EF. Generar un script no aplica cambios a una base.

## Límites y rollback

Los `processing` del escritor anterior pueden recuperarse después de diez minutos, dentro de la transacción. Los resultados antiguos sin huella no se reinterpretan como otra compra: devuelven conflicto y requieren revisar pedidos. No se purgan registros confirmados automáticamente.

La versión cubre contenido y precio de productos del carrito; no es una cotización congelada de tarifas de envío, comisiones o configuración bancaria. Las notificaciones siguen fuera del commit y pueden faltar si el proceso muere después de confirmar. Esa entrega requiere un outbox posterior. No se implementó compensación por cancelaciones ni reembolso: pertenecen a fases siguientes.

Si hay un problema de publicación, detener checkout. Mantener la columna nueva, pedidos y resultados confirmados; es un cambio aditivo y no exige borrar datos para retirar la aplicación. Volver al escritor anterior elimina las garantías de esta fase, por lo que no debe reabrirse checkout con él como recuperación automática. No ejecutar `Down` para borrar `RequestHash` mientras este API siga activo, ni restaurar un backup sobre compras posteriores sin conciliación.
