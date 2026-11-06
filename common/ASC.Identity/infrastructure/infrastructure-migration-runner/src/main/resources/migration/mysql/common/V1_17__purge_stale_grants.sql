SET SESSION lock_wait_timeout = 10;

SET @ddl = IF((SELECT COUNT(*) FROM information_schema.columns
               WHERE table_schema = DATABASE()
                 AND table_name = 'identity_authorizations'
                 AND column_name = 'purge_at') = 0,
              'ALTER TABLE identity_authorizations
                   ADD COLUMN purge_at DATETIME(6) AS (GREATEST(
                       COALESCE(modified_at, TIMESTAMP''1000-01-01 00:00:00''),
                       COALESCE(authorization_code_expires_at, TIMESTAMP''1000-01-01 00:00:00''),
                       COALESCE(access_token_expires_at, TIMESTAMP''1000-01-01 00:00:00''),
                       COALESCE(refresh_token_expires_at, TIMESTAMP''1000-01-01 00:00:00''),
                       COALESCE(id_token_expires_at, TIMESTAMP''1000-01-01 00:00:00''))) VIRTUAL',
              'DO 0');
PREPARE ddl FROM @ddl;
EXECUTE ddl;
DEALLOCATE PREPARE ddl;

SET @ddl = IF((SELECT COUNT(*) FROM information_schema.statistics
               WHERE table_schema = DATABASE()
                 AND table_name = 'identity_authorizations'
                 AND index_name = 'idx_identity_authorizations_purge_at') = 0,
              'ALTER TABLE identity_authorizations
                   ADD INDEX idx_identity_authorizations_purge_at (purge_at), ALGORITHM=INPLACE, LOCK=NONE',
              'DO 0');
PREPARE ddl FROM @ddl;
EXECUTE ddl;
DEALLOCATE PREPARE ddl;

SET @ddl = IF((SELECT COUNT(*) FROM information_schema.statistics
               WHERE table_schema = DATABASE()
                 AND table_name = 'identity_authorizations'
                 AND index_name = 'idx_identity_authorizations_modified_at') > 0,
              'ALTER TABLE identity_authorizations
                   DROP INDEX idx_identity_authorizations_modified_at, ALGORITHM=INPLACE, LOCK=NONE',
              'DO 0');
PREPARE ddl FROM @ddl;
EXECUTE ddl;
DEALLOCATE PREPARE ddl;

SET @ddl = IF((SELECT COUNT(*) FROM information_schema.statistics
               WHERE table_schema = DATABASE()
                 AND table_name = 'identity_consents'
                 AND index_name = 'idx_identity_consents_modified_at') = 0,
              'ALTER TABLE identity_consents
                   ADD INDEX idx_identity_consents_modified_at (modified_at), ALGORITHM=INPLACE, LOCK=NONE',
              'DO 0');
PREPARE ddl FROM @ddl;
EXECUTE ddl;
DEALLOCATE PREPARE ddl;

SET SESSION lock_wait_timeout = DEFAULT;

CREATE TABLE IF NOT EXISTS identity_purge_cursors (
    name VARCHAR(32) CHARACTER SET ascii NOT NULL,
    position DATETIME(6) NOT NULL,
    PRIMARY KEY (name)
) ENGINE=InnoDB;

INSERT IGNORE INTO identity_purge_cursors (name, position)
VALUES ('consents', TIMESTAMP'1000-01-01 00:00:00');

DROP EVENT IF EXISTS identity_purge_stale_grants;

DELIMITER //

CREATE EVENT identity_purge_stale_grants
    ON SCHEDULE EVERY 1 HOUR
    DO
    BEGIN
        DECLARE cutoff DATETIME(6) DEFAULT UTC_TIMESTAMP(6) - INTERVAL 30 DAY;
        DECLARE origin DATETIME(6) DEFAULT TIMESTAMP'1000-01-01 00:00:00';
        DECLARE removed INT;
        DECLARE chunks INT;
        DECLARE budget INT DEFAULT 100000;
        DECLARE client VARCHAR(36) CHARACTER SET ascii;
        DECLARE resume_at DATETIME(6);
        DECLARE started DATETIME(6);
        DECLARE slice_end DATETIME(6);
        DECLARE lapped INT DEFAULT 0;
        DECLARE finished INT DEFAULT 0;
        DECLARE done INT DEFAULT 0;
        DECLARE invalidated CURSOR FOR SELECT client_id FROM identity_clients WHERE is_invalidated = 1;
        DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

        SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED;

        IF GET_LOCK('identity_purge_stale_grants', 0) = 1 THEN
            OPEN invalidated;
            clients: LOOP
                FETCH invalidated INTO client;
                IF done = 1 OR budget <= 0 THEN
                    LEAVE clients;
                END IF;
                REPEAT
                    DELETE FROM identity_authorizations WHERE registered_client_id = client LIMIT 1000;
                    SET removed = ROW_COUNT(), budget = budget - removed;
                UNTIL removed < 1000 OR budget <= 0 END REPEAT;
                REPEAT
                    DELETE FROM identity_consents WHERE registered_client_id = client LIMIT 1000;
                    SET removed = ROW_COUNT(), budget = budget - removed;
                UNTIL removed < 1000 OR budget <= 0 END REPEAT;
            END LOOP;
            CLOSE invalidated;

            DELETE FROM identity_clients
            WHERE is_invalidated = 1
              AND NOT EXISTS (SELECT 1 FROM identity_authorizations a
                              WHERE a.registered_client_id = identity_clients.client_id)
              AND NOT EXISTS (SELECT 1 FROM identity_consents c
                              WHERE c.registered_client_id = identity_clients.client_id);

            SET chunks = 0;
            REPEAT
                DELETE FROM identity_authorizations
                WHERE purge_at < cutoff
                ORDER BY purge_at
                LIMIT 1000;
                SET removed = ROW_COUNT(), chunks = chunks + 1;
            UNTIL removed < 1000 OR chunks = 200 END REPEAT;

            DELETE FROM identity_consents
            WHERE modified_at IS NULL
              AND NOT EXISTS (SELECT 1 FROM identity_authorizations a
                              WHERE a.registered_client_id = identity_consents.registered_client_id
                                AND a.principal_id = identity_consents.principal_id)
            LIMIT 1000;

            SET resume_at = COALESCE(
                (SELECT p.position FROM identity_purge_cursors p WHERE p.name = 'consents'), origin);
            IF resume_at >= cutoff THEN
                SET resume_at = origin;
            END IF;
            SET started = resume_at, chunks = 0;
            REPEAT
                SET slice_end = (SELECT c.modified_at FROM identity_consents c
                             WHERE c.modified_at > resume_at AND c.modified_at < cutoff
                             ORDER BY c.modified_at
                             LIMIT 1 OFFSET 999);
                IF lapped = 1 AND (slice_end IS NULL OR slice_end >= started) THEN
                    SET slice_end = started, finished = 1;
                END IF;

                IF slice_end IS NULL THEN
                    DELETE FROM identity_consents
                    WHERE modified_at > resume_at AND modified_at < cutoff
                      AND NOT EXISTS (SELECT 1 FROM identity_authorizations a
                                      WHERE a.registered_client_id = identity_consents.registered_client_id
                                        AND a.principal_id = identity_consents.principal_id);
                    IF started = origin THEN
                        SET finished = 1;
                    END IF;
                    SET resume_at = origin, lapped = 1;
                ELSE
                    DELETE FROM identity_consents
                    WHERE modified_at > resume_at AND modified_at <= slice_end
                      AND NOT EXISTS (SELECT 1 FROM identity_authorizations a
                                      WHERE a.registered_client_id = identity_consents.registered_client_id
                                        AND a.principal_id = identity_consents.principal_id);
                    SET resume_at = slice_end;
                END IF;
                SET chunks = chunks + 1;
            UNTIL finished = 1 OR chunks = 100 END REPEAT;

            UPDATE identity_purge_cursors SET position = resume_at WHERE name = 'consents';

            DO RELEASE_LOCK('identity_purge_stale_grants');
        END IF;
    END //

DELIMITER ;

DROP EVENT IF EXISTS identity_delete_old_authorizations;
DROP EVENT IF EXISTS identity_delete_old_consents;
DROP EVENT IF EXISTS identity_delete_invalidated_clients;
DROP EVENT IF EXISTS identity_delete_invalidated_consents;
DROP EVENT IF EXISTS identity_delete_invalidated_authorization;
