START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261013164345_ZonasHorariasCitas') THEN
    ALTER TABLE "Comercios" ADD "ZonaHoraria" character varying(100) NOT NULL DEFAULT 'America/Mexico_City';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261013164345_ZonasHorariasCitas') THEN
    ALTER TABLE citas ADD "ZonaHoraria" character varying(100) NOT NULL DEFAULT 'America/Mexico_City';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261013164345_ZonasHorariasCitas') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261013164345_ZonasHorariasCitas', '8.0.4');
    END IF;
END $EF$;
COMMIT;

