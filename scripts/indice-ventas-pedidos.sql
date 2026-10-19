START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261019203321_IndiceVentasPedidos') THEN
    CREATE INDEX "IX_pedidos_EstadoPago_FechaCreacion_IdComercio" ON pedidos ("EstadoPago", "FechaCreacion", "IdComercio");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261019203321_IndiceVentasPedidos') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261019203321_IndiceVentasPedidos', '8.0.4');
    END IF;
END $EF$;
COMMIT;

