DROP TABLE IF EXISTS identity_client_authentication_methods_new;
DROP TABLE IF EXISTS identity_client_authentication_methods_old;

CREATE TABLE identity_client_authentication_methods_new (
    client_id varchar(36) not null,
    authentication_method enum('client_secret_post', 'none') not null,
    primary key (client_id, authentication_method)
) engine=InnoDB;

INSERT IGNORE INTO identity_client_authentication_methods_new (client_id, authentication_method)
SELECT client_id, authentication_method FROM identity_client_authentication_methods;

RENAME TABLE identity_client_authentication_methods TO identity_client_authentication_methods_old,
    identity_client_authentication_methods_new TO identity_client_authentication_methods;

DROP TABLE identity_client_authentication_methods_old;

ALTER TABLE identity_client_authentication_methods
    ADD CONSTRAINT FK_identity_client_authentication_methods_client_id
    FOREIGN KEY (client_id)
    REFERENCES identity_clients(client_id)
    ON DELETE CASCADE;

DROP TABLE IF EXISTS identity_client_redirect_uris_new;
DROP TABLE IF EXISTS identity_client_redirect_uris_old;

CREATE TABLE identity_client_redirect_uris_new (
    client_id varchar(36) not null,
    redirect_uri varchar(255) character set utf8mb4 collate utf8mb4_0900_bin not null,
    primary key (client_id, redirect_uri)
) engine=InnoDB;

INSERT IGNORE INTO identity_client_redirect_uris_new (client_id, redirect_uri)
SELECT client_id, redirect_uri FROM identity_client_redirect_uris;

RENAME TABLE identity_client_redirect_uris TO identity_client_redirect_uris_old,
    identity_client_redirect_uris_new TO identity_client_redirect_uris;

DROP TABLE identity_client_redirect_uris_old;

ALTER TABLE identity_client_redirect_uris
    ADD CONSTRAINT FK_identity_client_redirect_uris_client_id
    FOREIGN KEY (client_id)
    REFERENCES identity_clients(client_id)
    ON DELETE CASCADE;

DROP TABLE IF EXISTS identity_client_allowed_origins_new;
DROP TABLE IF EXISTS identity_client_allowed_origins_old;

CREATE TABLE identity_client_allowed_origins_new (
    client_id varchar(36) not null,
    allowed_origin varchar(255) character set utf8mb4 collate utf8mb4_0900_bin not null,
    primary key (client_id, allowed_origin)
) engine=InnoDB;

INSERT IGNORE INTO identity_client_allowed_origins_new (client_id, allowed_origin)
SELECT client_id, allowed_origin FROM identity_client_allowed_origins;

RENAME TABLE identity_client_allowed_origins TO identity_client_allowed_origins_old,
    identity_client_allowed_origins_new TO identity_client_allowed_origins;

DROP TABLE identity_client_allowed_origins_old;

ALTER TABLE identity_client_allowed_origins
    ADD CONSTRAINT FK_identity_client_allowed_origins_client_id
    FOREIGN KEY (client_id)
    REFERENCES identity_clients(client_id)
    ON DELETE CASCADE;

DROP TABLE IF EXISTS identity_client_scopes_new;
DROP TABLE IF EXISTS identity_client_scopes_old;

CREATE TABLE identity_client_scopes_new (
    client_id varchar(36) not null,
    scope_name varchar(255) not null,
    primary key (client_id, scope_name),
    index idx_identity_client_scopes_scope_name (scope_name)
) engine=InnoDB;

INSERT IGNORE INTO identity_client_scopes_new (client_id, scope_name)
SELECT client_id, scope_name FROM identity_client_scopes;

RENAME TABLE identity_client_scopes TO identity_client_scopes_old,
    identity_client_scopes_new TO identity_client_scopes;

DROP TABLE identity_client_scopes_old;

ALTER TABLE identity_client_scopes
    ADD CONSTRAINT FK_identity_client_scopes_client_id
    FOREIGN KEY (client_id)
    REFERENCES identity_clients(client_id)
    ON DELETE CASCADE,
    ADD CONSTRAINT FK_identity_client_scopes_scope_name
    FOREIGN KEY (scope_name)
    REFERENCES identity_scopes(name)
    ON DELETE CASCADE;
