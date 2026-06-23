DROP TABLE IF EXISTS identity_shedlock;
CREATE TABLE identity_shedlock (
    name VARCHAR(64) NOT NULL,
    lock_until TIMESTAMP(3) NOT NULL,
    locked_at TIMESTAMP(3) NOT NULL,
    locked_by VARCHAR(255) NOT NULL,
    PRIMARY KEY (name)
);

DROP TABLE IF EXISTS identity_certs;
CREATE TABLE identity_certs (
    id varchar(36) not null,
    created_at datetime(6),
    pair_type tinyint check (pair_type between 0 and 1) not null,
    private_key text not null,
    public_key text not null,
    primary key (id)
) engine=InnoDB;

DROP TABLE IF EXISTS identity_authorizations CASCADE;
DROP TABLE IF EXISTS identity_client_authentication_methods CASCADE;
DROP TABLE IF EXISTS identity_client_redirect_uris CASCADE;
DROP TABLE IF EXISTS identity_client_allowed_origins CASCADE;
DROP TABLE IF EXISTS identity_client_scopes CASCADE;
DROP TABLE IF EXISTS identity_consent_scopes CASCADE;
DROP TABLE IF EXISTS identity_consents CASCADE;
DROP TABLE IF EXISTS identity_clients CASCADE;
DROP TABLE IF EXISTS identity_scopes CASCADE;

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
    index idx_identity_clients_tenant_id (tenant_id)
) engine=InnoDB;

ALTER TABLE identity_clients
    ADD CONSTRAINT UK_client_id
    UNIQUE (client_id);

ALTER TABLE identity_clients
    ADD CONSTRAINT UK_client_secret
    UNIQUE (client_secret);

CREATE TABLE identity_scopes (
    name varchar(255) not null,
    `group` varchar(255) not null,
    `type` varchar(255) not null,
    primary key (name)
) engine=InnoDB;

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
    id_token_value TEXT,
    id_token_claims TEXT,
    id_token_metadata TEXT,
    id_token_issued_at DATETIME(6),
    id_token_expires_at DATETIME(6),
    modified_at datetime(6),
    primary key (principal_id, registered_client_id, authorization_grant_type),
    index idx_identity_authorizations_id (id)
) engine=InnoDB;

ALTER TABLE identity_authorizations
    ADD CONSTRAINT UK_id
    UNIQUE (id);

CREATE TABLE identity_client_authentication_methods (
    client_id varchar(36) not null,
    authentication_method enum('client_secret_post', 'none') not null,
    index idx_client_authentication_methods_client_id (client_id)
) engine=InnoDB;

CREATE TABLE identity_client_redirect_uris (
    client_id varchar(36) not null,
    redirect_uri tinytext not null,
    index idx_identity_client_redirect_uris_client_id (client_id)
) engine=InnoDB;

CREATE TABLE identity_client_allowed_origins (
    client_id varchar(36) not null,
    allowed_origin tinytext not null,
    index idx_identity_client_allowed_origins_client_id (client_id)
) engine=InnoDB;

CREATE TABLE identity_client_scopes (
    client_id varchar(36) not null,
    scope_name varchar(255) not null,
    index idx_identity_client_scopes_client_id (client_id),
    index idx_identity_client_scopes_scope_name (scope_name)
) engine=InnoDB;

CREATE TABLE identity_consents (
    registered_client_id varchar(36) not null,
    principal_id varchar(255) not null,
    is_invalidated tinyint(1) default false,
    modified_at datetime(6),
    primary key (registered_client_id, principal_id)
) engine=InnoDB;

CREATE TABLE identity_consent_scopes (
    registered_client_id varchar(36) not null,
    principal_id varchar(255) not null,
    scopes varchar(255) not null,
    primary key (registered_client_id, principal_id, scopes),
    index idx_identity_consent_scopes_scopes (scopes)
) engine=InnoDB;

ALTER TABLE identity_consent_scopes
    ADD CONSTRAINT FK_identity_consent_scopes_consents
    FOREIGN KEY (registered_client_id, principal_id)
    REFERENCES identity_consents(registered_client_id, principal_id)
    ON DELETE CASCADE;

CREATE TABLE IF NOT EXISTS audit_events (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    initiator VARCHAR(255),
    target VARCHAR(255),
    ip VARCHAR(255) NOT NULL,
    browser VARCHAR(255) NOT NULL,
    platform VARCHAR(255) NOT NULL,
    date TIMESTAMP NOT NULL,
    tenant_id BIGINT NOT NULL,
    user_id VARCHAR(255) NOT NULL,
    page VARCHAR(255) NOT NULL,
    action INT NOT NULL,
    description TEXT
) engine=InnoDB;

CREATE TABLE login_events (
    id bigint auto_increment primary key,
    login varchar(200) not null,
    active boolean not null default FALSE,
    ip varchar(50) not null,
    browser varchar(200) not null,
    platform varchar(200) not null,
    date timestamp not null,
    tenant_id bigint not null,
    user_id varchar(36) not null,
    page varchar(255),
    action integer not null default 0,
    description varchar(500)
) engine=InnoDB;

CREATE INDEX idx_login_events_user_id ON login_events(user_id);
CREATE INDEX idx_login_events_tenant_id ON login_events(tenant_id);

INSERT INTO identity_scopes (name, `group`, `type`) VALUES
('openid', 'identity', 'openid'),
('profile', 'identity', 'openid'),
('email', 'identity', 'openid'),
('files:read', 'files', 'resource'),
('files:write', 'files', 'resource'),
('accounts:read', 'accounts', 'resource'),
('accounts:write', 'accounts', 'resource'),
('rooms:read', 'rooms', 'resource'),
('rooms:write', 'rooms', 'resource');

