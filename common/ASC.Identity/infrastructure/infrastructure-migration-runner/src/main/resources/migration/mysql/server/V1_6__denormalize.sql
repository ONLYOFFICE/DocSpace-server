ALTER TABLE identity_authorizations
    DROP FOREIGN KEY FK_authorization_client_id;

ALTER TABLE identity_authorizations
    DROP INDEX idx_identity_authorizations_registered_client_id,
    DROP INDEX idx_identity_authorizations_principal_id,
    DROP INDEX idx_identity_authorizations_grant_type,
    DROP INDEX idx_identity_authorizations_is_invalidated;

ALTER TABLE identity_authorizations
    DROP COLUMN is_invalidated;

ALTER TABLE identity_authorizations
    ADD INDEX idx_identity_authorizations_id (id);

ALTER TABLE identity_clients
    DROP INDEX idx_identity_clients_is_invalidated;

ALTER TABLE identity_consents
    DROP FOREIGN KEY FK_identity_consents_client_id;

ALTER TABLE identity_consents
    DROP INDEX idx_identity_consents_registered_client_id,
    DROP INDEX idx_identity_consents_principal_id,
    DROP INDEX idx_identity_consents_is_invalidated;

ALTER TABLE identity_consent_scopes
    DROP FOREIGN KEY FK_identity_consent_scopes_scope_name;

ALTER TABLE identity_consent_scopes
    DROP FOREIGN KEY FK_identity_consent_scopes_consents;

ALTER TABLE identity_consent_scopes
    DROP PRIMARY KEY;

ALTER TABLE identity_consent_scopes
    CHANGE COLUMN scope_name scopes VARCHAR(255) NOT NULL;

ALTER TABLE identity_consent_scopes
    ADD PRIMARY KEY (registered_client_id, principal_id, scopes);

ALTER TABLE identity_consent_scopes
    ADD INDEX idx_identity_consent_scopes_scopes (scopes);

ALTER TABLE identity_consent_scopes
    ADD CONSTRAINT FK_identity_consent_scopes_scopes
        FOREIGN KEY (scopes)
        REFERENCES identity_scopes(name)
        ON DELETE CASCADE;

ALTER TABLE identity_consent_scopes
    ADD CONSTRAINT FK_identity_consent_scopes_consents
        FOREIGN KEY (registered_client_id, principal_id)
        REFERENCES identity_consents(registered_client_id, principal_id)
        ON DELETE CASCADE;

CREATE EVENT IF NOT EXISTS identity_delete_old_authorizations
ON SCHEDULE EVERY 1 DAY
ON COMPLETION PRESERVE
DO
DELETE FROM identity_authorizations
WHERE modified_at < NOW() - INTERVAL 30 DAY;

CREATE EVENT IF NOT EXISTS identity_delete_old_consents
ON SCHEDULE EVERY 1 DAY
ON COMPLETION PRESERVE
DO
DELETE FROM identity_consents
WHERE modified_at < NOW() - INTERVAL 30 DAY;