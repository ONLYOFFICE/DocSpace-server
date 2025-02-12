-- Drop all if exist
DROP EVENT IF EXISTS identity_delete_invalidated_clients;
DROP EVENT IF EXISTS identity_delete_invalidated_consents;
DROP EVENT IF EXISTS identity_delete_invalidated_authorization;

DROP TABLE IF EXISTS identity_authorizations CASCADE;
DROP TABLE IF EXISTS identity_client_authentication_methods CASCADE;
DROP TABLE IF EXISTS identity_client_redirect_uris CASCADE;
DROP TABLE IF EXISTS identity_client_allowed_origins CASCADE;
DROP TABLE IF EXISTS identity_client_scopes CASCADE;
DROP TABLE IF EXISTS identity_consent_scopes CASCADE;
DROP TABLE IF EXISTS identity_consents CASCADE;
DROP TABLE IF EXISTS identity_clients CASCADE;
DROP TABLE IF EXISTS identity_scopes CASCADE;

-- Create table for identity clients
CREATE TABLE identity_clients (
    client_id varchar(36) not null,
    tenant_id BIGINT not null,
    client_secret varchar(255) not null,
    name varchar(255),
    description LONGTEXT,
    logo LONGTEXT,
    website_url tinytext,
    terms_url tinytext,
    policy_url tinytext,
    logout_redirect_uri tinytext,
    is_public tinyint(1) default false,
    is_enabled tinyint(1) default true,
    is_invalidated tinyint(1) default false,
    created_on datetime(6),
    created_by varchar(255),
    modified_on datetime(6),
    modified_by varchar(255),
    version integer not null default 0,
    primary key (client_id),
    index idx_identity_clients_tenant_id (tenant_id),
    index idx_identity_clients_is_invalidated (is_invalidated)
) engine=InnoDB;

ALTER TABLE identity_clients
    ADD CONSTRAINT UK_client_id
    UNIQUE (client_id);

ALTER TABLE identity_clients
    ADD CONSTRAINT UK_client_secret
    UNIQUE (client_secret);

-- Create table for identity scopes
CREATE TABLE identity_scopes (
    name varchar(255) not null,
    `group` varchar(255) not null,
    `type` varchar(255) not null,
    primary key (name)
) engine=InnoDB;

-- Create table for identity authorizations
CREATE TABLE identity_authorizations (
    id varchar(255),
    registered_client_id varchar(36) not null,
    principal_id varchar(255) not null,
    tenant_id BIGINT not null,
    state varchar(500),
    attributes text,
    authorization_grant_type varchar(255),
    authorized_scopes text,
    authorization_code_value text,
    authorization_code_metadata varchar(255),
    authorization_code_issued_at datetime(6),
    authorization_code_expires_at datetime(6),
    access_token_type varchar(255),
    access_token_value text,
    access_token_hash text,
    access_token_scopes text,
    access_token_metadata text,
    access_token_issued_at datetime(6),
    access_token_expires_at datetime(6),
    refresh_token_value text,
    refresh_token_hash text,
    refresh_token_metadata text,
    refresh_token_issued_at datetime(6),
    refresh_token_expires_at datetime(6),
    is_invalidated tinyint(1) default false,
    modified_at datetime(6),
    primary key (principal_id, registered_client_id, authorization_grant_type),
    index idx_identity_authorizations_registered_client_id (registered_client_id),
    index idx_identity_authorizations_principal_id (principal_id),
    index idx_identity_authorizations_grant_type (authorization_grant_type),
    index idx_identity_authorizations_is_invalidated (is_invalidated)
) engine=InnoDB;

ALTER TABLE identity_authorizations
    ADD CONSTRAINT UK_id
    UNIQUE (id);

ALTER TABLE identity_authorizations
    ADD CONSTRAINT FK_authorization_client_id
    FOREIGN KEY (registered_client_id)
    REFERENCES identity_clients (client_id)
    ON DELETE CASCADE;

-- Create table for client authentication methods
CREATE TABLE identity_client_authentication_methods (
    client_id varchar(36) not null,
    authentication_method enum('client_secret_post', 'none') not null,
    index idx_client_authentication_methods_client_id (client_id)
) engine=InnoDB;

-- Create table for client redirect URIs
CREATE TABLE identity_client_redirect_uris (
    client_id varchar(36) not null,
    redirect_uri tinytext not null,
    index idx_identity_client_redirect_uris_client_id (client_id)
) engine=InnoDB;

-- Create table for client allowed origins
CREATE TABLE identity_client_allowed_origins (
    client_id varchar(36) not null,
    allowed_origin tinytext not null,
    index idx_identity_client_allowed_origins_client_id (client_id)
) engine=InnoDB;

-- Create table for identity client scopes
CREATE TABLE identity_client_scopes (
    client_id varchar(36) not null,
    scope_name varchar(255) not null,
    index idx_identity_client_scopes_client_id (client_id),
    index idx_identity_client_scopes_scope_name (scope_name)
) engine=InnoDB;

-- Create table for identity consents
CREATE TABLE identity_consents (
    registered_client_id varchar(36) not null,
    principal_id varchar(255) not null,
    is_invalidated tinyint(1) default false,
    modified_at datetime(6),
    primary key (registered_client_id, principal_id),
    index idx_identity_consents_registered_client_id (registered_client_id),
    index idx_identity_consents_principal_id (principal_id),
    index idx_identity_consents_is_invalidated (is_invalidated)
) engine=InnoDB;

-- Create join table for consent scopes
CREATE TABLE identity_consent_scopes (
    registered_client_id varchar(36) not null,
    principal_id varchar(255) not null,
    scope_name varchar(255) not null,
    index idx_identity_consent_scopes_registered_client_id (registered_client_id),
    index idx_identity_consent_scopes_principal_id (principal_id),
    index idx_identity_consent_scopes_scope_name (scope_name),
    primary key (registered_client_id, principal_id, scope_name)
) engine=InnoDB;

-- Create events for deleting invalidated records
CREATE EVENT IF NOT EXISTS identity_delete_invalidated_clients
ON SCHEDULE EVERY 1 hour
ON COMPLETION PRESERVE
    DO
DELETE FROM identity_clients ic WHERE ic.is_invalidated = 1;

CREATE EVENT IF NOT EXISTS identity_delete_invalidated_consents
ON SCHEDULE EVERY 1 hour
ON COMPLETION PRESERVE
    DO
DELETE FROM identity_consents ic WHERE ic.is_invalidated = 1;

CREATE EVENT IF NOT EXISTS identity_delete_invalidated_authorization
ON SCHEDULE EVERY 1 hour
ON COMPLETION PRESERVE
    DO
DELETE FROM identity_authorizations ia WHERE ia.is_invalidated = 1;

ALTER TABLE identity_client_authentication_methods
    ADD CONSTRAINT FK_identity_client_authentication_methods_client_id
    FOREIGN KEY (client_id)
    REFERENCES identity_clients(client_id)
    ON DELETE CASCADE;

ALTER TABLE identity_client_redirect_uris
    ADD CONSTRAINT FK_identity_client_redirect_uris_client_id
    FOREIGN KEY (client_id)
    REFERENCES identity_clients(client_id)
    ON DELETE CASCADE;

ALTER TABLE identity_client_allowed_origins
    ADD CONSTRAINT FK_identity_client_allowed_origins_client_id
    FOREIGN KEY (client_id)
    REFERENCES identity_clients(client_id)
    ON DELETE CASCADE;

ALTER TABLE identity_client_scopes
    ADD CONSTRAINT FK_identity_client_scopes_client_id
    FOREIGN KEY (client_id) REFERENCES identity_clients(client_id)
    ON DELETE CASCADE;

ALTER TABLE identity_client_scopes
    ADD CONSTRAINT FK_identity_client_scopes_scope_name
    FOREIGN KEY (scope_name) REFERENCES identity_scopes(name)
    ON DELETE CASCADE;

ALTER TABLE identity_consents
    ADD CONSTRAINT FK_identity_consents_client_id
    FOREIGN KEY (registered_client_id) REFERENCES identity_clients(client_id)
    ON DELETE CASCADE;

ALTER TABLE identity_consent_scopes
    ADD CONSTRAINT FK_identity_consent_scopes_consents
    FOREIGN KEY (registered_client_id, principal_id)
    REFERENCES identity_consents(registered_client_id, principal_id)
    ON DELETE CASCADE;

ALTER TABLE identity_consent_scopes
    ADD CONSTRAINT FK_identity_consent_scopes_scope_name
    FOREIGN KEY (scope_name) REFERENCES identity_scopes(name)
    ON DELETE CASCADE;
