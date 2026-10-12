-- Existing grants are cleared rather than backfilled, because the owner could only be derived by
-- joining identity_clients, which is not the source of truth in SaaS, where clients live in DynamoDB.
-- The cost is that users re-authorize once; in exchange no grant is left behind with an owner that
-- cannot be resolved. Consents are deleted rather than truncated because identity_consent_scopes
-- references them, and the delete cascades.
DELETE FROM identity_consents;

TRUNCATE TABLE identity_authorizations;

ALTER TABLE identity_authorizations
    ADD COLUMN owner_tenant_id BIGINT NULL,
    ADD COLUMN owner_user_id VARCHAR(255) NULL;

ALTER TABLE identity_consents
    ADD COLUMN owner_tenant_id BIGINT NULL,
    ADD COLUMN owner_user_id VARCHAR(255) NULL;

ALTER TABLE identity_authorizations
    ADD INDEX idx_identity_authorizations_owner (owner_tenant_id, owner_user_id);

ALTER TABLE identity_consents
    ADD INDEX idx_identity_consents_owner (owner_tenant_id, owner_user_id);
