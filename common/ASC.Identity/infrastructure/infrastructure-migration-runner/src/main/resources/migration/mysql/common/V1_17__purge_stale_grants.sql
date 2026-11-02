SET SESSION lock_wait_timeout = 10;

ALTER TABLE identity_authorizations
    ADD INDEX idx_identity_authorizations_modified_at (modified_at), ALGORITHM=INPLACE, LOCK=NONE;

ALTER TABLE identity_consents
    ADD INDEX idx_identity_consents_modified_at (modified_at), ALGORITHM=INPLACE, LOCK=NONE;

SET SESSION lock_wait_timeout = DEFAULT;

DROP EVENT IF EXISTS identity_purge_stale_grants;

DELIMITER //

CREATE EVENT identity_purge_stale_grants
    ON SCHEDULE EVERY 10 MINUTE
    DO
    BEGIN
        DECLARE cutoff DATETIME(6) DEFAULT UTC_TIMESTAMP(6) - INTERVAL 30 DAY;
        DECLARE removed INT;
        DECLARE chunks INT;

        SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED;

        IF GET_LOCK('identity_purge_stale_grants', 0) = 1 THEN
            SET chunks = 0;
            REPEAT
                DELETE FROM identity_authorizations
                WHERE registered_client_id IN (SELECT client_id FROM identity_clients WHERE is_invalidated = 1)
                LIMIT 1000;
                SET removed = ROW_COUNT(), chunks = chunks + 1;
            UNTIL removed < 1000 OR chunks = 50 END REPEAT;

            SET chunks = 0;
            REPEAT
                DELETE FROM identity_consents
                WHERE registered_client_id IN (SELECT client_id FROM identity_clients WHERE is_invalidated = 1)
                LIMIT 1000;
                SET removed = ROW_COUNT(), chunks = chunks + 1;
            UNTIL removed < 1000 OR chunks = 50 END REPEAT;

            DELETE FROM identity_clients
            WHERE is_invalidated = 1
              AND NOT EXISTS (SELECT 1 FROM identity_authorizations a
                              WHERE a.registered_client_id = identity_clients.client_id)
              AND NOT EXISTS (SELECT 1 FROM identity_consents c
                              WHERE c.registered_client_id = identity_clients.client_id);

            SET chunks = 0;
            REPEAT
                DELETE FROM identity_authorizations
                WHERE (modified_at IS NULL OR modified_at < cutoff)
                  AND GREATEST(
                        COALESCE(authorization_code_expires_at, '1000-01-01'),
                        COALESCE(access_token_expires_at, '1000-01-01'),
                        COALESCE(refresh_token_expires_at, '1000-01-01'),
                        COALESCE(id_token_expires_at, '1000-01-01')) < cutoff
                ORDER BY modified_at
                LIMIT 1000;
                SET removed = ROW_COUNT(), chunks = chunks + 1;
            UNTIL removed < 1000 OR chunks = 50 END REPEAT;

            SET chunks = 0;
            REPEAT
                DELETE FROM identity_consents
                WHERE (modified_at IS NULL OR modified_at < cutoff)
                  AND NOT EXISTS (SELECT 1 FROM identity_authorizations a
                                  WHERE a.registered_client_id = identity_consents.registered_client_id
                                    AND a.principal_id = identity_consents.principal_id)
                ORDER BY modified_at
                LIMIT 1000;
                SET removed = ROW_COUNT(), chunks = chunks + 1;
            UNTIL removed < 1000 OR chunks = 50 END REPEAT;

            DO RELEASE_LOCK('identity_purge_stale_grants');
        END IF;
    END //

DELIMITER ;

DROP EVENT IF EXISTS identity_delete_old_authorizations;
DROP EVENT IF EXISTS identity_delete_old_consents;
DROP EVENT IF EXISTS identity_delete_invalidated_clients;
DROP EVENT IF EXISTS identity_delete_invalidated_consents;
DROP EVENT IF EXISTS identity_delete_invalidated_authorization;
