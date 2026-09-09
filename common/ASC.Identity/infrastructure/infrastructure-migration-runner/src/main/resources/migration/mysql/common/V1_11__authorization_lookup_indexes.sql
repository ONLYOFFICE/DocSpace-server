ALTER TABLE identity_authorizations
    ADD INDEX idx_identity_authorizations_access_token_hash (access_token_hash(64)),
    ADD INDEX idx_identity_authorizations_refresh_token_hash (refresh_token_hash(64)),
    ADD INDEX idx_identity_authorizations_state (state),
    ADD INDEX idx_identity_authorizations_authorization_code_value (authorization_code_value(255)),
    ADD INDEX idx_identity_authorizations_registered_client_id (registered_client_id),
    ADD INDEX idx_identity_authorizations_tenant_id (tenant_id);
