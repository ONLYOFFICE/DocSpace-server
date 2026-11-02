DROP EVENT IF EXISTS identity_delete_old_authorizations;
DROP EVENT IF EXISTS identity_delete_old_consents;
DROP EVENT IF EXISTS identity_delete_invalidated_clients;
DROP EVENT IF EXISTS identity_delete_invalidated_consents;

DELETE FROM identity_consents WHERE is_invalidated = 1;

ALTER TABLE identity_authorizations
    ADD INDEX idx_identity_authorizations_modified_at (modified_at), ALGORITHM=INPLACE, LOCK=NONE;

ALTER TABLE identity_consents
    ADD INDEX idx_identity_consents_modified_at (modified_at), ALGORITHM=INPLACE, LOCK=NONE;

DROP PROCEDURE IF EXISTS identity_purge_stale_grants;

DELIMITER //

CREATE PROCEDURE identity_purge_stale_grants()
BEGIN
    DECLARE cutoff DATETIME(6) DEFAULT UTC_TIMESTAMP(6) - INTERVAL 30 DAY;
    DECLARE chunks INT DEFAULT 0;
    DECLARE removed INT DEFAULT 1;
    DECLARE invalidated VARCHAR(36);
    DECLARE clients INT DEFAULT 0;

    IF GET_LOCK('identity_purge_stale_grants', 0) = 1 THEN
        invalidated_clients: WHILE clients < 100 DO
            SET invalidated = NULL;
            SELECT client_id INTO invalidated FROM identity_clients WHERE is_invalidated = 1 LIMIT 1;
            IF invalidated IS NULL THEN
                LEAVE invalidated_clients;
            END IF;

            SET removed = 1;
            WHILE removed > 0 AND chunks < 50 DO
                DELETE FROM identity_authorizations WHERE registered_client_id = invalidated LIMIT 1000;
                SET removed = ROW_COUNT();
                SET chunks = chunks + 1;
            END WHILE;

            SET removed = 1;
            WHILE removed > 0 AND chunks < 50 DO
                DELETE FROM identity_consents WHERE registered_client_id = invalidated LIMIT 1000;
                SET removed = ROW_COUNT();
                SET chunks = chunks + 1;
            END WHILE;

            IF removed > 0 THEN
                LEAVE invalidated_clients;
            END IF;

            DELETE FROM identity_clients WHERE client_id = invalidated AND is_invalidated = 1;
            SET clients = clients + 1;
        END WHILE;

        SET chunks = 0;
        SET removed = 1;
        WHILE removed > 0 AND chunks < 50 DO
            DELETE FROM identity_authorizations
            WHERE (modified_at IS NULL OR modified_at < cutoff)
              AND GREATEST(
                    COALESCE(authorization_code_expires_at, '1000-01-01'),
                    COALESCE(access_token_expires_at, '1000-01-01'),
                    COALESCE(refresh_token_expires_at, '1000-01-01'),
                    COALESCE(id_token_expires_at, '1000-01-01')) < cutoff
            ORDER BY modified_at
            LIMIT 1000;
            SET removed = ROW_COUNT();
            SET chunks = chunks + 1;
        END WHILE;

        SET chunks = 0;
        SET removed = 1;
        WHILE removed > 0 AND chunks < 50 DO
            DELETE FROM identity_consents
            WHERE (registered_client_id, principal_id) IN (
                SELECT registered_client_id, principal_id FROM (
                    SELECT c.registered_client_id, c.principal_id
                    FROM identity_consents c
                    WHERE (c.modified_at IS NULL OR c.modified_at < cutoff)
                      AND NOT EXISTS (
                          SELECT 1 FROM identity_authorizations a
                          WHERE a.registered_client_id = c.registered_client_id
                            AND a.principal_id = c.principal_id)
                    ORDER BY c.modified_at
                    LIMIT 1000) stale);
            SET removed = ROW_COUNT();
            SET chunks = chunks + 1;
        END WHILE;

        DO RELEASE_LOCK('identity_purge_stale_grants');
    END IF;
END //

DELIMITER ;

DROP EVENT IF EXISTS identity_purge_stale_grants;

CREATE EVENT identity_purge_stale_grants
    ON SCHEDULE EVERY 10 MINUTE
    DO CALL identity_purge_stale_grants();
