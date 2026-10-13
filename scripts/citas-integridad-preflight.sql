-- Solo lectura. Ejecutar contra el destino identificado, antes de aplicar la fase 4.
-- Nunca cancelar, borrar ni desplazar citas automaticamente para hacer pasar la migracion.

-- Estados o intervalos que impiden instalar las restricciones.
SELECT "Uuid", "IdComercio", "FechaInicio", "FechaFin", "Estado"
FROM citas
WHERE "FechaFin" <= "FechaInicio" OR "Estado" NOT BETWEEN 1 AND 6;

-- Solapamientos por comercio, incluidos servicios distintos. Intervalos [inicio, fin).
SELECT a."Uuid" AS cita_a, b."Uuid" AS cita_b, a."IdComercio",
       a."FechaInicio" AS inicio_a, a."FechaFin" AS fin_a,
       b."FechaInicio" AS inicio_b, b."FechaFin" AS fin_b
FROM citas a
JOIN citas b ON b."IdComercio" = a."IdComercio" AND b."Id" > a."Id"
WHERE a."Estado" <> 5 AND b."Estado" <> 5
  AND a."FechaInicio" < b."FechaFin" AND a."FechaFin" > b."FechaInicio";

-- Asociaciones historicas incorrectas: revisar con el comercio antes de publicar.
SELECT c."Uuid", c."IdComercio", p.id_comercio AS comercio_del_servicio
FROM citas c JOIN "ProductosServicios" p ON p.id = c."IdProductoServicio"
WHERE c."IdComercio" <> p.id_comercio;

-- Los espacios materializados ya no deciden disponibilidad. Estas filas son evidencia
-- del escritor anterior y deben conciliarse; no eliminarlas sin conservar su origen.
SELECT s."Uuid", s."IdCita", c."Uuid" AS cita, s."Disponible"
FROM horarios_cita_servicio s
LEFT JOIN citas c ON c."Id" = s."IdCita"
WHERE (s."IdCita" IS NOT NULL AND (c."Id" IS NULL OR c."Estado" = 5 OR
       s."IdComercio" <> c."IdComercio" OR s."IdProductoServicio" <> c."IdProductoServicio" OR
       s."Fecha" <> c."FechaInicio"::date OR s."HoraInicio" <> c."FechaInicio"::time))
   OR (s."Disponible" = false AND s."IdCita" IS NULL);

-- Las fechas existentes son horas locales sin zona. Verificar estas muestras con los
-- registros del comercio: el SQL de migracion no desplaza las horas a UTC.
SELECT "Uuid", "IdComercio", "FechaInicio", "FechaFin", "Estado"
FROM citas ORDER BY "FechaInicio" DESC LIMIT 30;
