# Fase 3 — Pedidos, inventario y reembolsos

Estado al 12 de octubre de 2026: implementación y validación local completadas en API, panel y web del cliente. Migración, publicación y aceptación con proveedores en el entorno publicado pendientes. Se conservaron los cambios de las fases 1 y 2.

## Alcance y reglas

El pedido, sus movimientos de inventario, el registro de devolución, el historial y la comisión se modifican en una sola transacción PostgreSQL. Los escritores de estado, pago, comprobante y conciliación de comisiones bloquean la misma fila del pedido. Cada operación del panel exige la versión leída; una vista anterior recibe conflicto y debe actualizarse.

| Situación | Resultado |
|---|---|
| Rechazo o cancelación antes de preparar, sin pago confirmado ni comprobante pendiente de revisión | Libera exclusivamente las unidades descontadas al comprar, una vez |
| Cancelación durante preparación o cuando está listo, sin pago confirmado ni comprobante pendiente de revisión | Exige motivo y revisión de unidades recuperadas; solo repone las declaradas como aptas para vender |
| Pedido pagado | No permite cancelarlo por el cambio de estado habitual; requiere registrar la devolución del dinero |
| Pedido sin pago confirmado | No permite enviarlo, entregarlo ni completarlo |
| Reembolso antes de preparar | Cancela el pedido y libera su reserva registrada |
| Reembolso después de preparar o durante envío | Cancela el pedido; solo repone productos físicamente recuperados y declarados |
| Reembolso de pedido entregado o completado | Conserva el estado de cumplimiento y registra pago reembolsado; solo repone unidades declaradas |
| Producto eliminado, stock nulo o reposición que excedería el entero máximo | Conserva una incidencia de inventario pendiente; no fabrica existencias |
| Pedido antiguo sin dato de descuento | Conserva el dato desconocido y exige conciliación explícita con referencia/evidencia |

El checkout guarda por detalle el descuento real: cantidad comprada cuando se gestionaba stock, cero cuando no se gestionaba. `null` identifica datos antiguos desconocidos. No se rellena desde la configuración actual del producto. Para conciliar un detalle antiguo se indica cero o su cantidad completa original; la aplicación no puede comprobar por sí sola que la evidencia aportada sea verdadera.

En pedidos antiguos cancelados o rechazados también se exige declarar si la cancelación ocurrió antes de preparar y procede liberar reserva. Esa decisión se conserva y no puede cambiarse desde el formulario. Si no corresponde liberar reserva, conciliar el descuento no repone automáticamente mercancía consumida.

Una reposición confirmada tiene un movimiento único por detalle. La conciliación puede reintentar reposiciones que no llegaron a modificar stock, sin repetir las ya aplicadas. No se implementó una segunda devolución física posterior al reembolso: las unidades recuperadas deben declararse al registrar la operación. Una devolución tardía o una corrección de evidencia ya registrada necesita un procedimiento adicional, no editar directamente los contadores.

## Qué significa reembolsar

Los pedidos actuales usan efectivo o transferencia. El propietario registra una devolución **ya realizada fuera de la aplicación**: total exacto del pedido, incluido envío, método, referencia, motivo y unidades recuperadas. El formulario no inicia transferencias, no verifica un recibo bancario y no garantiza que el cliente haya recibido dinero. El cliente puede consultar monto, método, referencia, motivo y fecha, y tiene una indicación para contactar al comercio si no lo recibió.

Se admite un reembolso monetario total por pedido. No se implementaron reembolsos monetarios parciales, devoluciones automáticas a tarjeta ni reembolsos Stripe de suscripciones.

El API restringe reembolsos y conciliaciones al propietario del comercio; un colaborador no puede realizarlos. Estado y cobro mantienen su autorización comercial correspondiente. Las operaciones guardan usuario responsable, fecha, comentario y evento. El historial de conciliación interna de inventario no se expone al cliente.

## Reintentos y contabilidad

- El panel guarda clave UUID y payload exacto antes de enviar el registro de devolución, separados por cuenta, comercio y pedido. Si el almacenamiento falla, no envía la operación.
- Ante respuesta incierta, recarga o cierre, se conserva y reenvía la misma solicitud. El API devuelve el registro existente cuando coinciden clave y huella del contenido, incluso si la versión del pedido cambió después. Otra clave o contenido para un pedido ya reembolsado devuelve 409.
- Un reintento resuelve el registro informático; nunca significa devolver dinero otra vez. Descartar el intento local requiere revisar el pedido y no revierte la operación del servidor.
- Cobro confirmado y creación de comisión quedan en el mismo commit. Dos operadores no pueden cobrar dos veces ni mezclar cobro con cancelación.
- Un reembolso cancela una comisión pendiente que aún no integra un lote. Si ya fue pagada, conserva la venta histórica y genera un crédito por su comisión.
- Si la comisión está en un lote en revisión, el crédito queda retenido. Aprobar ese lote lo hace disponible; rechazarlo cancela juntos venta reembolsada y crédito retenido. No se concede saldo utilizable por una comisión que nunca fue pagada.
- Los créditos disponibles se arrastran hasta aplicarse y reducen el importe de futuras liquidaciones. Un saldo neto cero o negativo no genera un cobro. La interfaz distingue venta, crédito, cancelación y crédito en revisión.
- Las ediciones del catálogo envían su versión y utilizan la concurrencia nativa PostgreSQL `xmin`: una pantalla obsoleta no puede sobrescribir stock modificado por checkout o devolución.

El comprobante también se guarda bajo el bloqueo del pedido y no puede reabrir un pedido cerrado. La notificación se envía después del commit; si falla, el resultado persistido sigue siendo exitoso. Ante una respuesta incierta del guardado del comprobante se verifica su registro antes de borrar el objeto: si no puede verificarse, se conserva para evitar destruir un archivo potencialmente confirmado.

## Contratos y compatibilidad

Todos los endpoints conservan autenticación y `ApiResponse`:

| Endpoint | Requisitos nuevos |
|---|---|
| `PUT /api/PedidosComercio/{uuid}/estado?comercioId=...` | `versionPedido`; motivo al cerrar; `productosRestituibles` explícito durante preparación/listo |
| `PUT /api/PedidosComercio/{uuid}/pago?comercioId=...` | `versionPedido`; comprueba el comprobante actual o registra efectivo |
| `POST /api/PedidosComercio/{uuid}/reembolso?comercioId=...` | Propietario; `versionPedido`, `idempotencyKey`, `monto`, `metodo`, `referencia`, `motivo`, `productosDevueltos` |
| `PUT /api/PedidosComercio/{uuid}/inventario/conciliacion?comercioId=...` | Propietario; `versionPedido`, `referencia`, `stockDescontado`; `restituirReserva` para cierres antiguos sin política |

`productosDevueltos` y `stockDescontado` contienen `{ detalleUuid, cantidad }`. Para una devolución física sin productos recuperados se envía `[]`. Los clientes antiguos que omiten `versionPedido` reciben 400; no se toma cero implícito. Los formularios de productos también deben enviar la versión vigente al editar. Se requiere actualizar el panel y gestionar su caché PWA durante la publicación.

El detalle añade versión, política de reserva, descuento/reposición por producto, incidencia pendiente, acciones de pago/reembolso, registro de devolución y eventos del historial. Las restricciones se aplican en el API, aunque un cliente ignore esos campos.

## Evidencia local

| Comprobación | Resultado |
|---|---|
| API, suite Release completa | 358/358; cero omitidas |
| PostgreSQL 17 + PostGIS real, bases sintéticas por caso | 48/48 incluidas en la suite: 16 de checkout y 32 de esta fase |
| Compilación y publicación API Release | Cero errores y advertencias; salida sin configuración local privada |
| Panel | 71/71 pruebas; TypeScript, ESLint de archivos cambiados y build Vite/PWA aprobados |
| Web del cliente | 83/83 pruebas; ESLint y build Astro/Vercel aprobados |
| Astro check | 144 archivos; cero errores y warnings; 16 hints de deprecaciones existentes |
| Migración de fase 3 sobre datos sintéticos existentes | Aplicada dos veces; conserva datos antiguos y no añade ni elimina `xmin` |
| Revisión visual en navegador | Componentes reales del panel, callbacks sintéticos; formularios a 390 px y detalle a 390/1440 px |

Los casos PostgreSQL cubren carreras cobro/cancelación, cobros y reembolsos simultáneos, respuesta perdida, huella distinta, monto canónico, rollback tras modificar stock o fallar una comisión, devoluciones físicas parciales, mercancía consumida/en tránsito, conciliación antigua, reposición pendiente, comprobantes tardíos/simultáneos, edición obsoleta del catálogo, créditos retenidos/aprobados/rechazados, acceso propietario/colaborador/ajeno y contrato HTTP con JWT.

La vista visual de prueba está en `AdLocal/docs/fase3-ui-preview/`. Importa los componentes reales y el tema; los callbacks no llaman al API ni mueven dinero. Los nombres largos y la decisión sobre reserva se muestran completos por encima de los controles. Capturas guardadas en `evidencia/`: `reembolso-390.png`, `inventario-390.png`, `cancelacion-390.png`, `detalle-390.png` y `detalle-1440.png`.

Para reproducir la vista desde el repositorio del panel:

```powershell
node node_modules/vite/bin/vite.js --host 127.0.0.1 --port 5183 --strictPort
# Abrir http://127.0.0.1:5183/docs/fase3-ui-preview/?modo=reembolso
# Otros modos: inventario, cancelacion, detalle.
```

Para repetir la suite API se necesita una instancia PostgreSQL/PostGIS exclusivamente de prueba en loopback, con permiso de crear bases:

```powershell
$env:ADLOCAL_TEST_POSTGRES = 'Host=127.0.0.1;Port=55432;Database=postgres;Username=checkout_test;Pooling=false'
dotnet test AdLocalAPI.Tests/AdLocalAPI.Tests.csproj -c Release -p:UseAppHost=false
```

Los casos crean y eliminan sus bases aleatorias `adlocal_checkout_test_<guid>`. Sin la variable, los casos PostgreSQL se omiten explícitamente y no demuestran concurrencia. La instancia de esta entrega se creó desde cero con datos sintéticos; al concluir quedaron cero bases de prueba. No se usó la conexión productiva.

## Publicación y aceptación pendientes

1. Cerrar los pendientes externos de fase 1 e identificar base, API, panel, web y artefactos efectivos. Respaldar y verificar el procedimiento de recuperación. Las pruebas locales no sustituyen el ensayo de restore.
2. Ensayar primero en staging. Revisar `__EFMigrationsHistory`: se requiere la migración de fase 2 `20261012211320_CheckoutRequestHash`, además de las anteriores. Revisar `scripts/pedidos-inventario-reembolsos.sql`; es únicamente el delta de esta fase.
3. Pausar checkout, mutaciones de pedidos/comprobantes, catálogo y liquidación/revisión de comisiones. Drenar procesos anteriores: mezclar escritores antiguos con nuevos invalida las garantías de versión, descuento original y créditos.
4. Aplicar `20261012215314_PedidosInventarioReembolsos` antes de iniciar el API nuevo. El script aditivo no rellena stock histórico ni modifica campos de publicidad/eventos ajenos a la fase.
5. Publicar API, panel y web de forma coordinada. Comprobar actualización del service worker y pestañas antiguas del panel; no basta con ver el artefacto nuevo en el servidor.
6. Con cuentas aisladas, recorrer compra → aprobación → preparación → cobro → entrega → completado. Probar efectivo y transferencia, comprobante rechazado/reemplazado, cancelación antes/después de preparar y pérdida de respuesta al reembolso. Verificar estado, saldo, stock, referencia, actor, filas únicas y vista del cliente.
7. Probar devolución de productos recuperados, consumidos y en tránsito; pedido antiguo con evidencia; producto eliminado/stock nulo; lote pendiente/aprobado/rechazado y crédito disponible. Verificar que una cuenta ajena y un colaborador no pueden reembolsar ni conciliar.
8. Verificar con proveedores reales subida/lectura de comprobantes y entrega de notificaciones. En un piloto autorizado, comprobar el retorno externo del dinero con su referencia y el cliente, porque el API solo registra la declaración del comercio.
9. Reabrir operaciones tras esa aceptación. Vigilar 409/500, incidencias de stock, créditos retenidos, registros de devolución y almacenamiento pendiente del panel. La prueba de dos solicitantes no certifica carga, disponibilidad ni escalabilidad.

Para regenerar el delta sin aplicarlo a una base:

```powershell
# Proporcionar ConnectionStrings__DefaultConnection por entorno seguro.
dotnet ef migrations script 20261012211320_CheckoutRequestHash 20261012215314_PedidosInventarioReembolsos --project AdLocalAPI/AdLocalAPI.csproj --configuration Release --idempotent --output scripts/pedidos-inventario-reembolsos.sql
```

## Límites y recuperación

Las notificaciones siguen fuera del commit y pueden faltar si el proceso termina después de confirmar; falta un outbox con reintentos para garantizar su entrega. Los archivos conservados por un commit incierto necesitan conciliación posterior de objetos huérfanos. No se ejecutaron carga sostenida, ensayo de restauración, GitHub CI remoto, proveedores productivos ni una PWA instalada en teléfono.

Si la publicación falla, detener los escritores y conservar tablas, columnas, devoluciones, comisiones y movimientos. Preferir corregir hacia adelante o un artefacto compatible con el esquema y estas reglas. No ejecutar `Down` después de registrar movimientos: borra evidencia financiera y de inventario. Tampoco reabrir con el API antiguo, que desconoce devoluciones y créditos retenidos, ni restaurar un backup sobre operaciones posteriores sin conciliación.

La fase está completa como implementación local verificada. Su aceptación operacional permanece pendiente hasta ejecutar y documentar los pasos anteriores en el destino real.
