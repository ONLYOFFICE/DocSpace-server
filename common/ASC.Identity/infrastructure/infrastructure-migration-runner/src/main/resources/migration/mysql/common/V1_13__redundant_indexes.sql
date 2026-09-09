ALTER TABLE identity_clients
    DROP INDEX UK_client_id;

ALTER TABLE identity_consent_scopes
    DROP INDEX idx_identity_consent_scopes_registered_client_id,
    DROP INDEX idx_identity_consent_scopes_principal_id;
