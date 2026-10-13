-- Solo lectura, DESPUES de agregar las columnas ZonaHoraria y ANTES de reabrir citas.
-- America/Mexico_City es un valor inicial, no evidencia de la zona historica real.
SELECT co."Id", co."Nombre", co."ZonaHoraria",
       count(c."Id") FILTER (WHERE c."Estado" IN (1,2,3)) AS citas_activas
FROM "Comercios" co LEFT JOIN citas c ON c."IdComercio" = co."Id"
GROUP BY co."Id", co."Nombre", co."ZonaHoraria" ORDER BY co."Id";

-- Contrastar zonas desconocidas con el catalogo del proveedor y validar tambien .NET.
SELECT co."Id", co."Nombre", co."ZonaHoraria"
FROM "Comercios" co LEFT JOIN pg_timezone_names tz ON tz.name = co."ZonaHoraria"
WHERE tz.name IS NULL;

-- Una cita activa debe conservar una politica compatible con la agenda actual.
SELECT c."Uuid", c."Estado", c."FechaInicio", c."FechaFin",
       c."ZonaHoraria" AS zona_cita, co."ZonaHoraria" AS zona_comercio
FROM citas c JOIN "Comercios" co ON co."Id" = c."IdComercio"
WHERE c."Estado" IN (1,2,3) AND c."ZonaHoraria" <> co."ZonaHoraria";

-- Muestras para conciliar con registros del comercio. Este script no corrige ni
-- desplaza fechas, no adivina zonas y no crea historial de cambios inexistentes.
SELECT c."Uuid", co."Nombre", c."FechaInicio", c."FechaFin", c."Estado", c."ZonaHoraria"
FROM citas c JOIN "Comercios" co ON co."Id" = c."IdComercio"
ORDER BY c."FechaInicio" DESC LIMIT 50;
