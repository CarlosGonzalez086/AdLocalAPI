# Fase 4 — Integridad de las citas

Estado al 13 de octubre de 2026: implementación y validación local completadas en API, panel y web. No se aplicó la migración a producción ni se modificaron citas reales. Se conservaron las fases anteriores.

## Problemas corregidos

Antes, consultar disponibilidad creaba/eliminaba espacios. La reserva persistía la cita antes de ocupar el espacio, sin una transacción compartida. Dos solicitudes podían superar la consulta previa y reservar intervalos solapados. Las modificaciones de espacios no estaban garantizadas bajo el `NoTracking` global, el comercio podía asignar cualquier estado y los validadores comparaban horas locales con UTC. La pantalla de reserva permitía repetir una solicitud exitosa y no comprobaba un resultado completo antes de anunciar éxito.

Ahora la cita es la fuente de ocupación. `GET disponibilidad` es de solo lectura; calcula espacios con el horario y la duración vigentes, excluyendo intervalos ocupados en toda la agenda del comercio. Reservar o reprogramar vuelve a comprobar esos datos dentro de la transacción. Cambiar la duración no altera automáticamente una cita ya confirmada.

## Política que implementa esta entrega

- **Una agenda compartida por comercio**, incluso entre servicios diferentes. El modelo actual no representa recursos, profesionales ni cupos paralelos. Un nombre en `NombreAtiende` no crea otra capacidad de atención.
- **Zona horaria IANA configurable por comercio**, elegida en la pestaña Horarios. Cada cita conserva la zona con la que se reservó. El reloj de validación, el día de agenda y la indicación de abierto/cerrado del perfil público usan la zona del comercio; el navegador no desplaza la hora de la cita. Las fechas de auditoría sí son UTC.
- Fecha futura, minuto exacto sin sufijo de zona ni segundos adicionales, hasta 365 días. No hay turnos nocturnos que crucen medianoche. Duración válida de 1 a 1440 minutos, y el intervalo completo debe caber entre apertura y cierre.
- Servicio activo, visible, disponible, no eliminado y con modalidad de reservación; comercio activo y visible. Un servicio oculto/eliminado no admite reservas nuevas, pero su cita histórica sigue visible al cliente y la misma clave recupera la reserva existente.
- Intervalos `[inicio, fin)`: una cita puede comenzar al terminar otra. La cancelación libera ocupación; los demás estados conservan la evidencia histórica del intervalo.

El usuario solicitó zonas configurables por comercio. La ampliación reemplaza el supuesto inicial de una zona común. `America/Mexico_City` queda como valor inicial de configuración y de las filas existentes; **ese valor de migración no demuestra cuál era la zona histórica real**. Debe contrastarse con los registros de cada comercio antes de publicar. Ningún SQL de esta entrega mueve las fechas locales ni convierte automáticamente registros antiguos.

Después de agregar las columnas, ejecutar `scripts/citas-zonas-horarias-preflight.sql` para revisar configuración, citas activas y diferencias entre su zona y la agenda. Este diagnóstico es de solo lectura y también se validó contra PostgreSQL sintético. Una corrección histórica requiere evidencia y trazabilidad del responsable; no se aplica masivamente desde el catálogo ni se reinterpreta como un cambio cotidiano de zona.

## Configuración y horario estacional

El propietario puede buscar una región IANA y guardar su zona desde Horarios. Para comercios nuevos, la zona elegida se conserva al validar y enviar la creación. En comercios existentes se guarda mediante un comando independiente, protegido por propietario y zona anterior esperada. Un colaborador o propietario ajeno no puede cambiarla. Una edición general del comercio nunca sobrescribe la zona desde un objeto antiguo.

Se bloquea el cambio cuando hay citas pendientes, confirmadas o en atención, incluso si están vencidas y aún no fueron resueltas. Cancelar/reprogramar no es una operación implícita del cambio de zona. Una vez resueltas las citas, las históricas conservan su propia zona y las nuevas utilizan la configuración vigente. Los reintentos conservan también la zona original; no se reinterpretan los datos de una solicitud incierta con otra configuración.

Las reservas y reprogramaciones envían la zona vista por el usuario. El API la compara bajo el bloqueo del comercio: si cambió durante la selección, devuelve conflicto. Disponibilidad excluye horas inexistentes, repetidas y intervalos cuyo cambio estacional altera la duración real frente a la duración local; el escritor rechaza esos mismos casos. No se escoge silenciosamente una de las dos ocurrencias de una hora repetida.

El selector se comprobó en una vista móvil sintética: buscar `America/Tijuana`, seleccionarla y guardar actualiza la zona mostrada. Captura: `AdLocal/docs/fase4-ui-preview/evidencia/zona-configuracion-390.png`. También se corrigió el domingo del formulario: es día 0, no 7, conforme al contrato .NET.

## Escrituras atómicas y concurrencia

Crear una reserva bloquea primero al cliente para vincular su clave, después al comercio y finalmente lee el producto bajo un bloqueo compartido. Reprogramar, cancelar y cambiar estado bloquean comercio y cita. Las escrituras de horarios usan el mismo bloqueo del comercio; no permiten dejar citas vigentes fuera de atención. Cita, espacio reservado/liberado y auditoría se confirman juntos o se revierten juntos.

Además del bloqueo del escritor, PostgreSQL instala una restricción de exclusión GiST: no admite dos intervalos no cancelados que se solapen dentro del mismo comercio. Esta defensa sigue actuando ante una escritura que omita el repositorio. Se basa en [rangos y restricciones de exclusión de PostgreSQL](https://www.postgresql.org/docs/17/rangetypes.html#RANGETYPES-CONSTRAINT) y requiere la extensión [btree_gist](https://www.postgresql.org/docs/17/btree-gist.html).

La tabla `horarios_cita_servicio` se conserva como representación derivada del intervalo reservado, no como fuente de disponibilidad. Un espacio antiguo con duración/disponibilidad obsoleta no puede hacer aparecer un turno ocupado ni bloquear uno libre. Las operaciones actualizan o liberan los espacios vinculados en la misma transacción; no se purga automáticamente evidencia del escritor anterior.

Cada cita tiene `Version`. Cambios de estado, cancelación y reprogramación requieren la versión leída. Si otro operador/cliente la modificó, el API devuelve 409 y no pisa el cambio. Los terminales no pueden reabrirse. El historial `cita_historial` registra evento, estado/fecha anterior y nueva, usuario, comentario y fecha UTC. No se fabrica historial de citas anteriores a esta migración.

## Ciclo permitido

| Estado actual | Transiciones |
|---|---|
| Pendiente | Confirmada, Cancelada, No asistió |
| Confirmada | En atención, Cancelada, No asistió |
| En atención | Completada |
| Completada, Cancelada, No asistió | Ninguna |

`En atención` y `No asistió` requieren que haya comenzado el horario. `Completada` requiere que haya terminado. El panel recibe acciones filtradas por esos tiempos; puede actualizar quién atiende sin cambiar un estado activo. Una cita en atención no se presenta como cerrada aunque todavía no pueda completarse.

El cliente solo puede cancelar o reprogramar una cita propia, pendiente/confirmada y todavía futura. Cancelar exige motivo. Reprogramar conserva la cita y su espacio anteriores si el nuevo turno falla; si tiene éxito vuelve a pendiente para nueva confirmación del comercio. La consulta de reprogramación puede excluir la propia cita, pero el API comprueba que pertenece al usuario y al servicio; no permite excluir reservas ajenas.

## Reserva recuperable e interfaces

La creación exige una clave UUID explícita. Se guarda un resultado por cliente y clave, vinculado a servicio, fecha, persona y notas mediante huella SHA-256. Misma clave y contenido recuperan la cita existente, incluso si después se canceló; mismo identificador con otro contenido devuelve conflicto. No se reabre la cita cancelada al recuperar una solicitud.

La web guarda clave y datos originales por cuenta antes de enviar. Ante pérdida de conexión, respuesta incompleta o recarga, los conserva y permite comprobarlos desde el formulario o `Mis citas`. Si el almacenamiento está bloqueado/corrupto, no sustituye silenciosamente la clave. Cerrar una comprobación local exige revisar la lista; no cancela la cita en el servidor. Se requiere respuesta 200 con UUID antes de anunciar éxito, y el formulario cierra tras confirmar.

Las consultas de horarios descartan respuestas de una fecha anterior cuando cambia la selección. El cliente ve cancelar/reprogramar según el permiso devuelto por el API, conserva los datos del formulario si falla y puede actualizar el estado antes de reintentar. El panel envía versión, mantiene el error visible y actualiza la agenda ante conflicto. Las fechas y horas se muestran como hora del comercio, sin convertirlas al huso de un navegador que esté de viaje.

## Contrato

Se conserva `Controller → Service → Repository`, autenticación por rol y `ApiResponse`.

| Endpoint | Contrato nuevo |
|---|---|
| `GET /api/Citas/disponibilidad/{productoUuid}?fecha=YYYY-MM-DD` | Solo lectura; opcional `citaUuid` propia para reprogramar |
| `POST /api/Citas` | `idempotencyKey`, `productoUuid`, `fechaInicio` local, `zonaHoraria`, `nombrePersona`, `notas` opcional |
| `PUT /api/Citas/{uuid}/cancelar` | Cuerpo JSON con `versionCita` y `motivo`; ya no motivo únicamente en query |
| `PUT /api/Citas/{uuid}/reprogramar` | `versionCita`, `fechaInicio` local, `zonaHoraria` |
| `PUT /api/Citas/{uuid}?comercioId=...` | `versionCita`, estado permitido, nombre de atención/motivo según operación |
| `PUT /api/Comercios/{id}/zona-horaria` | Propietario; `zonaHoraria`, `zonaHorariaAnterior` |

El DTO añade versión, zona horaria, acciones y permiso de gestión del cliente. Los contratos de comercio público, comercio propio y selector de agenda incluyen la zona. Los clientes publicados anteriores deben actualizarse; omitir versión, zona de cita o clave produce 400. No se reinterpretan timestamps con `Z` como hora local. Una solicitud local anterior a esta ampliación que carezca de zona requiere revisar Mis citas y cerrar explícitamente su comprobación; no se le agrega una zona ni otra clave silenciosamente.

## Evidencia local

| Comprobación | Resultado |
|---|---|
| API Release completa | **393/393**, cero omitidas |
| PostgreSQL 17 + PostGIS real, bases independientes por caso | **80/80** incluidas: 48 de fases anteriores y 32 de citas |
| Pruebas específicas de citas | 49: 32 PostgreSQL y 17 unitarias |
| Panel | **77/77**, TypeScript, ESLint de archivos cambiados y build Vite/PWA aprobados |
| Web | **96/96**, ESLint y build Astro/Vercel aprobados |
| Astro check | Cero errores y warnings; 15 hints de deprecaciones existentes |
| API publish Release | Cero errores/advertencias; salida sin archivos privados locales |
| Control de credenciales rastreadas y diff check | Aprobados; no equivalen a rotación externa de fase 1 |

Las pruebas PostgreSQL cubren dos clientes por un turno, servicios distintos solapados, intervalos contiguos, reservas/reintentos simultáneos, clave con otro contenido, fecha UTC/segundos/pasado/cierre, disponibilidad sin escrituras y duración cambiada, cancelación, reprogramación exitosa/fallida/concurrente, rollback después de insertar cita o liberar su espacio, estados/timing, cliente/comercio ajeno, servicio eliminado, restricción contra inserción externa y contrato HTTP con JWT.

La migración de integridad y la ampliación de zonas se aplicaron dos veces a datos sintéticos existentes conservando sus horas. Otra prueba incluye un conflicto histórico y comprueba que la migración de integridad falla sin borrar citas ni añadir parcialmente la versión. Los casos multizona prueban Tijuana/Cancún, cambio de zona contra una reserva simultánea, conservación del histórico y del reintento, edición general obsoleta, rechazo de zona ajena/inválida, horario estacional y contrato HTTP del comando del propietario.

Se revisaron componentes reales en vistas aisladas: panel a 390 px; web a 390/1280 px. Se comprobó selección de transiciones, reprogramación completa y cancelación que elimina acciones posteriores. Los callbacks son sintéticos y no llaman a la API. Estas vistas no sustituyen un ensayo completo autenticado con el destino publicado ni una PWA en dispositivo.

Las vistas están en `AdLocal/docs/fase4-ui-preview/` y `AdLocalWeb/docs/fase4-ui-preview/`. Capturas en `AdLocal/docs/fase4-ui-preview/evidencia/`. Para reproducir, iniciar Vite en loopback desde el repositorio correspondiente y abrir `/docs/fase4-ui-preview/`; `?modo=terminal` en el panel muestra una cita cancelada.

La suite usa `ADLOCAL_TEST_POSTGRES` con una conexión PostgreSQL/PostGIS exclusivamente de prueba en loopback y permiso de crear bases. Sin esa variable, los casos PostgreSQL se omiten explícitamente. Se utilizó una instancia nueva con datos sintéticos y quedaron cero bases de prueba al concluir.

## Despliegue y aceptación pendientes

1. Resolver los pendientes externos de fase 1 e identificar artefactos, base y URLs efectivas. Respaldar y disponer de recuperación verificada. Confirmar la capacidad de una agenda por comercio y determinar con evidencia la zona de cada comercio y sus citas antiguas.
2. Ejecutar primero `scripts/citas-integridad-preflight.sql` en el destino identificado. Revisar estados/intervalos inválidos, pares solapados, asociaciones históricas y espacios desincronizados; contrastar muestras de hora con el comercio. No borrar ni desplazar citas para que el SQL pase.
3. Conciliar conflictos históricos con decisiones documentadas del responsable. La nueva restricción incluye todos los intervalos no cancelados, también históricos; no basta con comprobar solo las citas futuras.
4. Ensayar staging con la migración anterior `20261012215314_PedidosInventarioReembolsos` ya aplicada. Verificar que el proveedor permite `btree_gist`. Revisar el delta `scripts/citas-integridad.sql`: no cambia publicidad/eventos ni desplaza horas.
5. Pausar reservas, cambios de citas, horarios y edición de servicios; drenar solicitudes del API anterior. No mezclar escritores antiguos con los nuevos durante la publicación.
6. Aplicar en orden `20261013155802_IntegridadCitas` y `20261013164345_ZonasHorariasCitas`, usando sus deltas revisados. Antes de reabrir, conciliar explícitamente las zonas iniciales con evidencia del responsable: el default CDMX no es una inferencia histórica válida. Si existen citas activas antiguas en otra zona, esa conciliación debe hacerse durante la preparación controlada del despliegue, documentando responsable, evidencia y filas; el comando cotidiano bloquea desplazar citas activas. Publicar API, panel y web coordinadamente y comprobar renovación del service worker/pestañas antiguas. Ambas migraciones deben existir antes de iniciar el API nuevo.
7. Con cuentas aisladas, probar dos clientes por el mismo turno, dos servicios que se solapan, dos operadores con la misma versión, cancelación, reprogramación y pérdida de respuesta seguida de recarga/recuperación. Verificar cita, espacio, versión, auditoría y ausencia de duplicados.
8. Verificar horas en un servidor UTC y navegadores de otra zona, con comercios de Tijuana y Cancún. Probar cambio estacional, reserva simultánea con cambio de zona, fecha seleccionada bajo una zona obsoleta, cierre, duración modificada, servicio oculto/eliminado y horario que excluiría una cita vigente. Recorrer pendiente → confirmada → atención → completada con el reloj real.
9. Reabrir después de aceptación documentada; vigilar conflictos, errores y reservas pendientes. Las pruebas de dos solicitudes no certifican carga sostenida ni disponibilidad comercial.

Para regenerar solo el delta, sin aplicarlo a una base:

```powershell
# Proporcionar ConnectionStrings__DefaultConnection por entorno seguro.
dotnet ef migrations script 20261012215314_PedidosInventarioReembolsos 20261013155802_IntegridadCitas --project AdLocalAPI/AdLocalAPI.csproj --configuration Release --idempotent --output scripts/citas-integridad.sql
dotnet ef migrations script 20261013155802_IntegridadCitas 20261013164345_ZonasHorariasCitas --project AdLocalAPI/AdLocalAPI.csproj --configuration Release --idempotent --output scripts/citas-zonas-horarias.sql
```

## Límites y recuperación

No se agregaron agenda por profesional, cupos simultáneos, turnos nocturnos, notificaciones recordatorias ni carga sostenida. Tampoco se verificaron producción, CI remoto, restore o dispositivo instalado. La edición general del comercio conserva su flujo previo con varios guardados: proteger horario y zona no convierte toda esa edición y sus imágenes en una transacción única. Las métricas de pedidos de otras fases conservan su política previa; esta ampliación aplica a horarios y citas.

Si la publicación falla, detener escritores y corregir con un artefacto compatible. Conservar restricción, claves, versiones y auditoría; no ejecutar `Down` después de registrar operaciones, porque elimina el historial y la recuperación de reservas. No reabrir con el API anterior ni restaurar un backup sobre citas posteriores sin conciliación.

La entrega está completa como implementación local verificada. La aceptación operacional requiere cerrar los pasos de despliegue y ensayo anteriores.
