CREATE TABLE identity_authorizations (
	principal_name varchar(255) not null,
	registered_client_id varchar(255) not null,
	access_token_expires_at datetime(6),
	access_token_issued_at datetime(6),
	access_token_metadata text,
	access_token_scopes varchar(1000),
	access_token_type varchar(255),
	access_token_value text,
	attributes text,
	authorization_code_expires_at datetime(6),
	authorization_code_issued_at datetime(6),
	authorization_code_metadata varchar(255),
	authorization_code_value text,
	authorization_grant_type varchar(255),
	authorized_scopes varchar(1000),
	id varchar(255),
	invalidated tinyint(1),
	modified_at datetime(6),
	refresh_token_expires_at datetime(6),
	refresh_token_issued_at datetime(6),
	refresh_token_metadata text,
	refresh_token_value text,
	state varchar(500),
	primary key (principal_name, registered_client_id)
) engine=InnoDB;

ALTER TABLE identity_authorizations
	ADD CONSTRAINT UK_id
	UNIQUE (id);

CREATE TABLE identity_clients (
	client_id varchar(36) not null,
	allowed_origins tinytext,
	authentication_method varchar(100),
	client_issued_at datetime(6),
	client_secret varchar(255),
	created_by varchar(255),
	created_on datetime(6),
	description tinytext,
	enabled tinyint(1),
	invalidated tinyint(1),
	logo LONGTEXT,
	logout_redirect_uri tinytext,
	modified_by varchar(255),
	modified_on datetime(6),
	client_name varchar(255),
	policy_url tinytext,
	redirect_uris tinytext,
	scopes tinytext,
	tenant_id integer,
	tenant_url tinytext,
	terms_url tinytext,
	website_url tinytext,
	primary key (client_id)
) engine=InnoDB;

CREATE TABLE identity_consents (
	principal_name varchar(255) not null,
	registered_client_id varchar(255) not null,
	invalidated tinyint(1),
	modified_at datetime(6),
	scopes tinytext,
	client_client_id varchar(36),
	primary key (principal_name, registered_client_id)
) engine=InnoDB;

ALTER TABLE identity_clients
	ADD CONSTRAINT UK_client_id
	UNIQUE (client_id);

ALTER TABLE identity_clients
	ADD CONSTRAINT UK_client_secret
	UNIQUE (client_secret);

ALTER TABLE identity_consents
	ADD CONSTRAINT FK_client_id
	FOREIGN KEY (client_client_id)
	REFERENCES identity_clients (client_id)
	ON DELETE CASCADE;

ALTER TABLE identity_consents
	ADD CONSTRAINT FK_authorization_id
	FOREIGN KEY (principal_name, registered_client_id)
	REFERENCES identity_authorizations (principal_name, registered_client_id)
	ON DELETE CASCADE;

ALTER TABLE identity_authorizations
	ADD CONSTRAINT FK_authorization_client_id
	FOREIGN KEY (registered_client_id)
	REFERENCES identity_clients (client_id)
	ON DELETE CASCADE;

CREATE EVENT IF NOT EXISTS identity_delete_invalidated_clients
ON SCHEDULE EVERY 1 hour
ON COMPLETION PRESERVE
    DO
      DELETE FROM identity_clients ic WHERE ic.invalidated = 1;

CREATE EVENT IF NOT EXISTS identity_delete_invalidated_consents
ON SCHEDULE EVERY 1 hour
ON COMPLETION PRESERVE
    DO
      DELETE FROM identity_consents ic WHERE ic.invalidated = 1;

CREATE EVENT IF NOT EXISTS identity_delete_invalidated_authorization
ON SCHEDULE EVERY 1 hour
ON COMPLETION PRESERVE
    DO
      DELETE FROM identity_authorizations ia WHERE ia.invalidated = 1;

DROP TRIGGER IF EXISTS identity_update_entry_clients;

CREATE TRIGGER identity_update_entry_clients
BEFORE UPDATE ON identity_clients
FOR EACH ROW
BEGIN
  IF new.modified_on <= old.modified_on
  THEN
   SIGNAL SQLSTATE '02000' SET MESSAGE_TEXT = 'Warning: updated date can not be before than existing date!';
  END IF;
END;

DROP TRIGGER IF EXISTS identity_update_entry_authorizations;

CREATE TRIGGER identity_update_entry_authorizations
BEFORE UPDATE ON identity_authorizations
FOR EACH ROW
BEGIN
  IF new.modified_at <= old.modified_at
  THEN
   SIGNAL SQLSTATE '02000' SET MESSAGE_TEXT = 'Warning: updated date can not be before than existing date!';
  END IF;
END;

DROP TRIGGER IF EXISTS identity_update_entry_consents;

CREATE TRIGGER identity_update_entry_consents
BEFORE UPDATE ON identity_consents
FOR EACH ROW
BEGIN
  IF new.modified_at <= old.modified_at
  THEN
   SIGNAL SQLSTATE '02000' SET MESSAGE_TEXT = 'Warning: updated date can not be before than existing date!';
  END IF;
END;
