ALTER TABLE identity_consents
    ADD INDEX idx_identity_consents_principal_id (principal_id);
