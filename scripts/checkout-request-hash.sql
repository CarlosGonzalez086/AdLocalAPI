START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012211320_CheckoutRequestHash') THEN
    ALTER TABLE checkout_idempotencias ADD "RequestHash" character varying(64);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012211320_CheckoutRequestHash') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261012211320_CheckoutRequestHash', '8.0.4');
    END IF;
END $EF$;
COMMIT;

